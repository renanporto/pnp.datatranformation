﻿using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using NUnit.Framework;
using Newtonsoft.Json;
using Vtex.Practices.DataTransformation.Exceptions;
using Vtex.Practices.DataTransformation.Extensions;
using Vtex.Practices.DataTransformation.ServiceModel;

namespace Vtex.Practices.DataTransformation.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestAutoMapper()
        {
            var columnMapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var properties = typeof(DummyDto).GetProperties();

            Assert.AreEqual(properties.Count(), columnMapper.Columns.Count);
        }

        [Test]
        public void TestManualMapper()
        {
            var columnMapper = ColumnMapper<DummyDto>.Factory
                                .CreateNew(true)
                                .MapColumn("Name", "NewName1", CellType.STRING, ToUpperCase)
                                .MapColumn("Name", ToUpperCase)
                                .MapColumn(1, CommaToDashReplacer);

            var properties = typeof(DummyDto).GetProperties();

            Assert.AreEqual(properties.Count(), columnMapper.Columns.Count);
        }

        [Test]
        public void TestXlsCreationFromDtoCollection_AutoMap()
        {
            var columnMapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var handler = columnMapper.DataHandler;

            var generatedData = GenerateData();

            var workBook = handler.EncodeDataToWorkbook(generatedData);

            Assert.DoesNotThrow(() => handler.WriteToFile(@"C:\Temp\TestAutomap.xls", workBook));
        }

        [Test]
        public void TestXlsCreationFromDtoCollection_ManualMap()
        {
            var columnMapper = ColumnMapper<DummyDto>.Factory
                                .CreateNew(true)
                                .MapColumn("Name", "NewName2", CellType.STRING, ToUpperCase)
                                .MapColumn(1, value => value.ToString().Replace(',', '-'))
                                .MapColumn(0, "NewColumnName");

            var handler = columnMapper.DataHandler;
            var generatedData = GenerateData();
            var workBook = handler.EncodeDataToWorkbook(generatedData);

            Assert.DoesNotThrow(() => handler.WriteToFile(@"C:\Temp\TestManualmap.xls", workBook));
        }

        [Test]
        public void ShouldImportFileTest()
        {
            const string filePath = @"C:\Temp\TestAutomap.xls";

            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var handler = mapper.DataHandler;

            var result = handler.DecodeFileToDtoCollection(filePath);

            Assert.IsNotNull(result);
        }

        [Test]
        public void ShouldImportCustomFileTest()
        {
            const string filePath = @"C:\Temp\planilha_productEspec.xls";

            var mapper = ColumnMapper<ProductsSpecificationByCategoryIdDto>.Factory
                    .CreateNew(true)
                    .MapColumn("ProductId", "IdProduto (não alterável)")
                    .MapColumn("ProductName", "NomeProduto (não alterável)")
                    .MapColumn("FieldId", "IdCampo (não alterável)")
                    .MapColumn("FieldName", "NomeCampo (não alterável)")
                    .MapColumn("FieldTypeName", "NomeTipoCampo (não alterável)")
                    .MapColumn("FieldValueId", "IdCampoValor (não alterável)")
                    .MapColumn("FieldValueName", "NomeCampoValor (não alterável)")
                    .MapColumn("ProductFieldValueId", "CodigoEspecificaCao (não alterável)")
                    .MapColumn("ProductFieldValueText", "ValorEspecificaCao")
                    .MapColumn("ProductRefId", "CodigoReferencia (não alterável)");

            var handler = new DataHandler<ProductsSpecificationByCategoryIdDto>(mapper);

            var result = handler.DecodeFileToDtoCollection(filePath);

            Assert.IsNotNull(result);
        }

        [Test]
        public void ShouldPerformEndToEndTest()
        {
            const string filePath = @"C:\Temp\end2endtest.xls";

            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var handler = mapper.DataHandler;

            var expectedData = GenerateData();

            var workBook = handler.EncodeDataToWorkbook(expectedData);

            handler.WriteToFile(filePath, workBook);

            var result = handler.DecodeFileToDtoCollection(filePath);

            var serializedResult = JsonConvert.SerializeObject(result);

            var serializedData = JsonConvert.SerializeObject(expectedData);

            Assert.AreEqual(serializedResult, serializedData);
        }

        [Test]
        public void InvalidPropertyMappingTest()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            Assert.Throws<InvalidPropertyException>(() => mapper.MapColumn("InvalidPropertyName"));
        }

        [Test]
        public void InvalidColumnMappingTest()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            Assert.Throws<InvalidPropertyException>(() => mapper.MapColumn("InvalidPropertyName", ToUpperCase));
        }

        [Test]
        public void ColumnMappingWithColumnObject()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew();
            var column = new Column
            {
                PropertyName = "IenumerableOfInts",
                HeaderText = "Header Name",
                ListSeparator = "-"
            };

            mapper.MapColumn(column);

            Assert.AreEqual(1, mapper.Columns.Count);
        }

        [Test]
        public void ColumnMappingWithColumnObjectWithoutHeaderText()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew();
            var column = new Column
            {
                PropertyName = "Name"
            };

            mapper.MapColumn(column);

            Assert.AreEqual(column.PropertyName, mapper.Columns[0].HeaderText);
        }

        [Test]
        public void ShouldEncodeXlsStreamTest()
        {
            var data = GenerateData();

            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var handler = mapper.DataHandler;

            var stream = handler.EncodeToXlsStream(data);

            Assert.IsNotNull(stream);
        }

        [Test]
        public void ShouldEncodeCsvStreamTest()
        {
            var data = GenerateData();

            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew(true);

            var handler = mapper.DataHandler;

            var stream = handler.EncodeToCsvStream(data);

            Assert.IsNotNull(stream);
        }

        [Test]
        public void ExportHelperXlsTest()
        {
            var data = GenerateData();
            var xlsData = ExportHelper.Export(data).AsXls();
            Assert.IsNotNull(xlsData);
        }

        [Test]
        public void ExportHelperCsvTest()
        {
            var data = GenerateData();
            var csvData = ExportHelper.Export(data, ColumnMapper<DummyDto>.Factory.CreateNew()).AsCsv();
            Assert.IsNull(csvData);
        }

        [Test]
        public void ParameterTypeInferenceAndCustomTransformTest()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew();
            mapper.MapColumn("Name", ToUpperCase);

            var mappedColumn = mapper.Columns.FirstOrDefault();

            Assert.IsNotNull(mappedColumn);
            Assert.AreEqual(mappedColumn.PropertyName, mappedColumn.HeaderText);
            Assert.AreEqual(CellType.STRING, mappedColumn.CellType);
        }

        [Test]
        public void ParameterTypeInferenceTest()
        {
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew();
            mapper.MapColumn("Name");

            var mappedColumn = mapper.Columns.FirstOrDefault();

            Assert.IsNotNull(mappedColumn);
            Assert.AreEqual("Name", mappedColumn.PropertyName);
            Assert.AreEqual(mappedColumn.PropertyName, mappedColumn.HeaderText);
            Assert.AreEqual(CellType.STRING, mappedColumn.CellType);
        }

        [Test]
        public void InvalidColumnMappingWithComplexParameters()
        {
            //MapColumn(int index, string propertyName, string headerText, CellType cellType,
            //                            Func<object, object> customTranformationAction)
            var mapper = ColumnMapper<DummyDto>.Factory.CreateNew();

            Assert.Throws<InvalidPropertyException>(() => mapper.MapColumn(1, "InvalidPropertyName", "", CellType.Unknown, null));
        }

        [Test]
        public void ShouldPerformSelfTest()
        {
            const string input = "Hello";
            var output = ToUpperCase(input);

            const string input2 = "one,two";
            var output2 = CommaToDashReplacer(input2);

            Assert.AreEqual(input.ToUpper(), output);
            Assert.AreEqual(input2.Replace(",", "-"), output2);
        }

        public List<DummyDto> GenerateData()
        {
            return DummyDtoFactory.GenerateData(500).ToList();
        }

        private static object ToUpperCase(object value)
        {
            return value.ToString().ToUpper();
        }

        private static object CommaToDashReplacer(object value)
        {
            return value.ToString().Replace(',', '-');
        }
    }

    public class ProductsSpecificationByCategoryIdDto
    {
        public int? ProductId { get; set; }
        public string ProductName { get; set; }
        public int? FieldId { get; set; }
        public string FieldName { get; set; }
        public string FieldTypeName { get; set; }
        public string FieldValueId { get; set; }
        public string FieldValueName { get; set; }
        public string ProductFieldValueId { get; set; }
        public string ProductFieldValueText { get; set; }
        public string ProductRefId { get; set; }
    }
}