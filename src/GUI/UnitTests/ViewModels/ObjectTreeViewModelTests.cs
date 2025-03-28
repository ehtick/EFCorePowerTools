﻿using NUnit.Framework;

namespace UnitTests.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EFCorePowerTools.Contracts.ViewModels;
    using EFCorePowerTools.ViewModels;
    using GalaSoft.MvvmLight.Messaging;
    using Moq;
    using NUnit.Framework.Legacy;
    using RevEng.Common;

    [TestFixture]
    public class ObjectTreeViewModelTests
    {
        [Test]
        public void Constructors_ArgumentNullException_ViewModelFactory()
        {
            // Arrange
            Func<ISchemaInformationViewModel> s = null;
            Func<ITableInformationViewModel> t = null;
            Func<IColumnInformationViewModel> c = null;

            // Act & Assert
            ClassicAssert.Throws<ArgumentNullException>(() => new ObjectTreeViewModel(s, t, c));
        }

        [Test]
        public void Constructors_CollectionsInitialized()
        {
            // Act
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);

            // Assert
            ClassicAssert.IsNotNull(vm.Types);
        }

        [Test]
        public void AddObjects_Null()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);

            // Act
            // Assert
            ClassicAssert.Throws<ArgumentNullException>(() => vm.AddObjects(null, null));
        }

        [Test]
        public void AddObjects_EmptyCollection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var objectsToAdd = new TableModel[0];

            // Act
            vm.AddObjects(objectsToAdd, null);

            // Assert
            ClassicAssert.IsEmpty(vm.Types.SelectMany(t => t.Schemas));
        }

        [Test]
        public void AddObjects_Collection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var objects = GetDatabaseObjects().OrderBy(c => c.Schema).ThenBy(c => c.Name).ToArray();
            var replacers = GetReplacers();

            // Act
            vm.AddObjects(objects, replacers);
            var vmobjects = vm.Types.SelectMany(t => t.Schemas).SelectMany(c => c.Objects).OrderBy(c => c.Schema).ThenBy(c => c.Name).ToArray();

            // Assert
            ClassicAssert.IsNotEmpty(vm.Types.SelectMany(t => t.Schemas).SelectMany(t => t.Objects));
            ClassicAssert.AreEqual(objects.Length, vm.Types.SelectMany(t => t.Schemas).SelectMany(t => t.Objects).Count());
            for (var i = 0; i < objects.Count(); i++)
            {
                AreObjectsEqual(objects[i], vmobjects[i]);
                ClassicAssert.IsFalse(vm.Types.SelectMany(t => t.Schemas).SelectMany(t => t.Objects).ElementAt(i).IsSelected);
            }
        }

        [Test]
        public void AddObjects_Replacers()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var objects = GetDatabaseObjects().ToArray();
            var replacers = GetReplacers();

            // Act
            vm.AddObjects(objects, replacers);
            var vmobjects = vm.Types.SelectMany(t => t.Schemas).SelectMany(c => c.Objects).ToArray();
            vm.SetSelectionState(true);
            var renamers = vm.GetRenamedObjects();

            // Assert
            ClassicAssert.AreEqual(replacers.Length, renamers.Count());

            foreach (var replacerSchema in replacers)
            {
                foreach (var table in replacerSchema.Tables)
                {
                    var vmobject = vmobjects.First(o => o.Schema == replacerSchema.SchemaName && o.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase));
                    ClassicAssert.AreEqual(vmobject.NewName, table.NewName);
                    foreach (var column in table.Columns)
                    {
                        ClassicAssert.AreEqual(vmobject.Columns.First(c => c.Name.Equals(column.Name, StringComparison.OrdinalIgnoreCase)).NewName, column.NewName);
                    }
                }
            }
        }

        [Test]
        public void AddObjects_Replacers_Issue679()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);

            var objects = new TableModel[3];
            objects[0] = new TableModel("departmentdetail", null, DatabaseType.Mysql, ObjectType.Table, new List<ColumnModel> { new ColumnModel("DEPTCode", null, false, false) }.ToArray());
            objects[1] = new TableModel("employeedetail", null, DatabaseType.Mysql, ObjectType.Table, new List<ColumnModel> { new ColumnModel("EMPCode", null, false, false) }.ToArray());
            objects[2] = new TableModel("same", null, DatabaseType.Mysql, ObjectType.Table, new List<ColumnModel> { new ColumnModel("same", null, false, false) }.ToArray());

            var replacers = new Schema[1];
            replacers[0] = new Schema()
            {
                SchemaName = null,
                UseSchemaName = false,
                Tables = new List<TableRenamer>
                {
                    new TableRenamer
                    {
                        Name = "departmentdetail",
                        NewName = "DepartmentDetail",
                        Columns = new List<ColumnNamer>
                        {
                            new ColumnNamer { Name = "DEPTCode", NewName = "DEPTCode" },
                        },
                    },
                    new TableRenamer
                    {
                        Name = "employeedetail",
                        NewName = "EmployeeDetail",
                        Columns = new List<ColumnNamer>
                        {
                            new ColumnNamer { Name = "EMPCode", NewName = "EMPCode" },
                        },
                    },
                    new TableRenamer
                    {
                        Name = "same",
                        NewName = "same",
                        Columns = new List<ColumnNamer>(),
                    },
                },
            };

            // Act
            vm.AddObjects(objects, replacers);
            vm.SetSelectionState(true);
            var renamers = vm.GetRenamedObjects().ToList();

            // Assert
            ClassicAssert.AreEqual(3, renamers[0].Tables.Count);
            ClassicAssert.AreEqual(1, renamers[0].Tables[0].Columns.Count);
            ClassicAssert.AreEqual(1, renamers[0].Tables[1].Columns.Count);
        }

        [Test]
        public void SelectObjects_Null()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);

            // Act and assert
            ClassicAssert.Throws<ArgumentNullException>(() => vm.SelectObjects(null));
        }

        [Test]
        public void SelectTables_EmptyCollection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var selectedTables = new SerializationTableModel[0];

            // Act
            vm.SelectObjects(selectedTables);

            // Assert
            ClassicAssert.IsEmpty(vm.GetSelectedObjects());
        }

        [Test]
        public void SelectTables_Collection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            vm.AddObjects(GetDatabaseObjects(), null);

            // Act
            var selectedObjects = GetSelectedObjects().ToList();
            vm.SelectObjects(selectedObjects);

            // Assert
            ClassicAssert.AreEqual(GetSelectedObjects().Count(), vm.GetSelectedObjects().Count());
            for (var i = 0; i < vm.GetSelectedObjects().Count(); i++)
            {
                var a = vm.GetSelectedObjects().ElementAt(i);
                var b = selectedObjects[i];

                ClassicAssert.AreEqual(a.Name, b.Name);
                ClassicAssert.AreEqual(a.ObjectType, b.ObjectType);
                for (var j = 0; j < a.ExcludedColumns?.Count(); j++)
                {
                    ClassicAssert.AreEqual(a.ExcludedColumns.ElementAt(0), b.ExcludedColumns.ElementAt(0));
                }
            }
        }

        [Test]
        public void SearchText_NoDirectFilter()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);
            var preFilter = vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects);

            // Act
            vm.Search("ref", SearchMode.Text);

            // Assert
            ClassicAssert.AreEqual(databaseObjects.Length, preFilter.Count());
            var postFilter = vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects).Where(c => c.IsVisible);
            ClassicAssert.AreEqual(2, postFilter.Count());
        }

        [Test]
        public void SearchText_FilterAfterDelay()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            vm.Search("ref", SearchMode.Text);

            // Assert
            ClassicAssert.That(
                () =>
                {
                    var postFilter = vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects).Where(c => c.IsVisible);
                    return postFilter.Count() < databaseObjects.Length && postFilter.All(m => m.Name.ToUpper().Contains("REF"));
                },
                Is.True.After(1500, 200));
        }

        [Test]
        public void GetSelectedObjects_NoTables()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);

            // Act
            var result = vm.GetSelectedObjects();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.IsEmpty(result);
        }

        [Test]
        public void GetSelectedObjects_WithObjects_NoneSelected()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            vm.AddObjects(GetDatabaseObjects(), null);

            // Act
            var result = vm.GetSelectedObjects();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.IsEmpty(result);
        }

        [Test]
        public void GetSelectedObjects_WithObjects_AllSelected()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects().OrderBy(c => c.Schema).ThenBy(c => c.Name).ToArray();
            vm.AddObjects(databaseObjects, null);
            foreach (var item in vm.Types)
            {
                item.SetSelectedCommand.Execute(true);
            }

            // Act
            var result = vm.GetSelectedObjects().OrderBy(c => c.Name).ToArray();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(11, result.Count());
            for (var i = 0; i < result.Length - 1; i++)
            {
                ClassicAssert.AreEqual(databaseObjects[i + 1].DisplayName, result[i].Name);
            }

            // Act
            var renamed = vm.GetRenamedObjects();
            ClassicAssert.AreEqual(0, renamed.Count());
        }

        [Test]
        public void GetSelectedObjects_WithObjects_PartialSelection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);
            foreach (var item in vm.Types.SelectMany(c => c.Schemas).OrderBy(c => c.Name))
            {
                item.Objects[0].SetSelectedCommand.Execute(true);
            }

            // Act
            var result = vm.GetSelectedObjects().ToList();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(6, result.Count);
            foreach (var item in vm.Types.SelectMany(c => c.Schemas).OrderBy(c => c.Name))
            {
                ClassicAssert.IsTrue(result.Exists(c => c.Name == item.Objects[0].ModelDisplayName));
            }
        }

        [Test]
        public void GetSelectionState_NoneSelected()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            foreach (var item in vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects))
            {
                item.SetSelectedCommand.Execute(true);
            }

            // Assert
            ClassicAssert.IsTrue(vm.GetSelectionState());
        }

        [Test]
        public void GetSelectionState_AllSelected()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            foreach (var item in vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects))
            {
                item.SetSelectedCommand.Execute(false);
            }

            // Assert
            ClassicAssert.IsFalse(vm.GetSelectionState());
        }

        [Test]
        public void GetSelectionState_PartialSelection()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            foreach (var item in vm.Types.SelectMany(c => c.Schemas).SelectMany(c => c.Objects).Take(1))
            {
                item.SetSelectedCommand.Execute(true);
            }

            // Assert
            ClassicAssert.IsNull(vm.GetSelectionState());
        }

        [Test]
        public void SetSelectionState([Values(true, false)] bool selected)
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            vm.SetSelectionState(selected);

            // Assert
            if (selected)
            {
                ClassicAssert.AreEqual(databaseObjects.Length, vm.GetSelectedObjects().Count());
            }
            else
            {
                ClassicAssert.AreEqual(0, vm.GetSelectedObjects().Count());
            }
        }

        [Test]
        public void GetRenamedObjects()
        {
            // Arrange
            var vm = new ObjectTreeViewModel(CreateSchemaInformationViewModelMockObject, CreateTableInformationViewModelMockObject, CreateColumnInformationViewModelMockObject);
            var databaseObjects = GetDatabaseObjects();
            vm.AddObjects(databaseObjects, null);

            // Act
            vm.Types[0].Schemas[0].Objects[0].SetSelectedCommand.Execute(true);
            vm.Types[0].Schemas[0].Objects[0].NewName = "NewTableName";
            vm.Types[0].Schemas[0].Objects[0].Columns[0].NewName = "NewColumnName";

            vm.Types[0].Schemas[3].Objects[0].SetSelectedCommand.Execute(true);
            vm.Types[0].Schemas[3].Objects[0].NewName = "DepartmentDetail";

            vm.Types[0].Schemas[3].Objects[0].Columns[0].SetSelectedCommand.Execute(true);
            vm.Types[0].Schemas[3].Objects[0].Columns[0].NewName = "DepartmentName";

            // Assert
            var renamedObjects = vm.GetRenamedObjects();
            ClassicAssert.IsNotNull(renamedObjects);
            ClassicAssert.AreSame("NewTableName", renamedObjects.First().Tables[0].NewName);
            ClassicAssert.AreSame("NewColumnName", renamedObjects.First().Tables[0].Columns[0].NewName);

            ClassicAssert.AreSame("DepartmentDetail", renamedObjects.Last().Tables[0].NewName);
            ClassicAssert.AreSame("DepartmentName", renamedObjects.Last().Tables[0].Columns[0].NewName);
        }

        private static TableModel[] GetDatabaseObjects()
        {
            IEnumerable<ColumnModel> CreateColumnsWithId()
            {
                return new[]
                {
                    new ColumnModel("Id", null, true, false),
                    new ColumnModel("column1", null, false, false),
                    new ColumnModel("column2", null, false, false),
                };
            }

            IEnumerable<ColumnModel> CreateColumnsWithoutId()
            {
                return new[]
                {
                    new ColumnModel("Id", null, false, false),
                    new ColumnModel("column1", null, false, false),
                    new ColumnModel("column2", null, false, false),
                };
            }

            var r = new TableModel[11];

            r[0] = new TableModel("Atlas", "dbo", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[1] = new TableModel("RefactorLog", "__", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[2] = new TableModel("__RefactorLog", "dbo", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[3] = new TableModel("sysdiagrams", "dbo", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[4] = new TableModel("test", "unit", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[5] = new TableModel("foo", "unit", DatabaseType.SQLServer, ObjectType.Table, CreateColumnsWithId());
            r[6] = new TableModel("view1", "views", DatabaseType.SQLServer, ObjectType.View, CreateColumnsWithoutId());
            r[7] = new TableModel("view2", "views", DatabaseType.SQLServer, ObjectType.View, CreateColumnsWithoutId());
            r[8] = new TableModel("procedure1", "stored", DatabaseType.SQLServer, ObjectType.Procedure, new ColumnModel[0]);
            r[9] = new TableModel("procedure2", "stored", DatabaseType.SQLServer, ObjectType.Procedure, new ColumnModel[0]);
            r[10] = new TableModel("departmentdetail", null, DatabaseType.Mysql, ObjectType.Table, new List<ColumnModel> { new ColumnModel("departmentname", null, false, false), new ColumnModel("DEPTCode", null, false, false), new ColumnModel("Id", null, true, false) }.ToArray());
            return r;
        }

        private static SerializationTableModel[] GetSelectedObjects()
        {
            var r = new SerializationTableModel[5];

            r[0] = new SerializationTableModel("[dbo].[Atlas]", ObjectType.Table, new[] { "column1" }, null);
            r[1] = new SerializationTableModel("[unit].[test]", ObjectType.Table, new string[0], null);
            r[2] = new SerializationTableModel("[unit].[foo]", ObjectType.Table, new string[0], null);
            r[3] = new SerializationTableModel("[views].[view1]", ObjectType.View, new string[0], null);
            r[4] = new SerializationTableModel("[stored].[procedure2]", ObjectType.Procedure, new string[0], null);
            return r;
        }

        private static Schema[] GetReplacers()
        {
            var r = new Schema[3];

            r[0] = new Schema()
            {
                ColumnPatternReplaceWith = "ColumnPatternReplaceWith",
                ColumnRegexPattern = "ColumnRegexPattern",
                SchemaName = "dbo",
                TablePatternReplaceWith = "TablePatternReplaceWith",
                TableRegexPattern = "TableRegexPattern",
                Tables = new List<TableRenamer>
                {
                    new TableRenamer
                    {
                        Name = "atlas",
                        NewName = "newatlas",
                        Columns = new List<ColumnNamer>
                        {
                            new ColumnNamer { Name = "column1", NewName = "newcolumn1" },
                        },
                    },
                },
            };

            r[1] = new Schema()
            {
                SchemaName = "other",
                UseSchemaName = true,
            };

            r[2] = new Schema()
            {
                SchemaName = null,
                UseSchemaName = false,
                Tables = new List<TableRenamer>
                {
                    new TableRenamer
                    {
                        Name = "departmentdetail",
                        NewName = "DepartmentDetail",
                        Columns = new List<ColumnNamer>
                        {
                            new ColumnNamer { Name = "departmentname", NewName = "DepartmentName" },
                            new ColumnNamer { Name = "DEPTCode", NewName = "DEPTCode" },
                        },
                    },
                },
            };

            return r;
        }

        private static ISchemaInformationViewModel CreateSchemaInformationViewModelMockObject()
        {
            return new SchemaInformationViewModel();
        }

        private static ITableInformationViewModel CreateTableInformationViewModelMockObject()
        {
            var messenger = new Mock<IMessenger>();
            messenger.SetupAllProperties();
            return new TableInformationViewModel(messenger.Object);
        }

        private static IColumnInformationViewModel CreateColumnInformationViewModelMockObject()
        {
            var messenger = new Mock<IMessenger>();
            messenger.SetupAllProperties();
            return new ColumnInformationViewModel(messenger.Object);
        }

        private static void AreObjectsEqual(TableModel a, ITableInformationViewModel b)
        {
            ClassicAssert.AreEqual(a.Name, b.Name);
            ClassicAssert.AreEqual(a.Schema, b.Schema);
            ClassicAssert.AreEqual(a.ObjectType, b.ObjectType);
            for (var i = 0; i < a.Columns.Count(); i++)
            {
                ClassicAssert.AreEqual(a.Columns.ElementAt(i).Name, b.Columns[i].Name);
            }
        }
    }
}