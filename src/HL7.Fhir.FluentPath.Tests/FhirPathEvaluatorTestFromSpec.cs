﻿/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

extern alias dstu2;

using dstu2::Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.FluentPath;


using boolean = System.Boolean;
using DecimalType = dstu2::Hl7.Fhir.Model.FhirDecimal; // System.Decimal;
using UriType = dstu2::Hl7.Fhir.Model.FhirUri;
using dstu2::Hl7.Fhir.Serialization;
using System.IO;
using Hl7.Fhir.Tests.FhirPath;
using System.Xml;
using Hl7.FluentPath.Support;
using System.Xml.Linq;
using Hl7.FluentPath.Functions;

static class ConverterExtensions
{
    public static ResourceReference getSubject(this Order me)
    {
        return me.Subject;
    }

    public static void setValue(this Quantity me, double? value)
    {
        if (value.HasValue)
            me.Value = (decimal)value.Value;
        else
            me.Value = null;
    }
    public static void setUnit(this Quantity me, string value)
    {
        me.Unit = value;
    }
    public static void setCode(this Quantity me, string value)
    {
        me.Code = value;
    }
    public static void setSystem(this Quantity me, string value)
    {
        me.System = value;
    }
    public static void setValueSet(this ElementDefinition.BindingComponent me, Element value)
    {
        me.ValueSet = value;
    }
    public static Element getValueSet(this ElementDefinition.BindingComponent me)
    {
        return me.ValueSet;
    }

    public static Range setLow(this Range me, SimpleQuantity value)
    {
        me.Low = value;
        return me;
    }
    public static Range setHigh(this Range me, SimpleQuantity value)
    {
        me.High = value;
        return me;
    }

    public static RiskAssessment.PredictionComponent addPrediction(this RiskAssessment me)
    {
        var item = new RiskAssessment.PredictionComponent();
        me.Prediction.Add(item);
        return item;
    }
    public static List<RiskAssessment.PredictionComponent> getPrediction(this RiskAssessment me)
    {
        return me.Prediction;
    }

    public static Element getProbability(this RiskAssessment.PredictionComponent me)
    {
        return me.Probability;
    }
    public static RiskAssessment.PredictionComponent setProbability(this RiskAssessment.PredictionComponent me, Element value)
    {
        me.Probability = value;
        return me;
    }

}


[TestClass]
public class FluentPathTests
{
    private void test(Resource resource, String expression, IEnumerable<XElement> expected)
    {
        var tpXml = FhirSerializer.SerializeToXml(resource);
        var npoco = new ModelNavigator(resource);
        //       FhirPathEvaluatorTest.Render(npoco);

        IEnumerable<IValueProvider> actual = PathExpression.Select(expression, FluentValueList.Create(npoco));
        Assert.AreEqual(expected.Count(), actual.Count());

        expected.Zip(actual, compare).Count();
    }

    private static bool compare(XElement expected, IValueProvider actual)
    {
        var type = expected.Attribute("type").Value;        
        var tp = (ITypeNameProvider)actual;
        Assert.AreEqual(type, tp.TypeName, "incorrect output type");

        if (expected.IsEmpty) return true;      // we are not checking the value

        var value = expected.Value;
        Assert.AreEqual(value, actual.ToStringRepresentation(), "incorrect output value");

        return true;
    }


    // @SuppressWarnings("deprecation")
    private void testBoolean(Resource resource, Base focus, String focusType, String expression, boolean value)
    {
        var input = ModelNavigator.CreateInput(focus);
        var container = resource != null ? ModelNavigator.CreateInput(resource) : null;

        Assert.IsTrue(PathExpression.IsBoolean(expression, value, input, container));
    }


    enum ErrorType
    {
        Syntax,
        Semantics
    }

    private void testInvalid(Resource resource, ErrorType type, String expression)
    {
        try
        {
            PathExpression.Select(expression, FluentValueList.Create(new ModelNavigator(resource)));
            Assert.Fail();
        }
        catch(FormatException)
        {
            if (type != ErrorType.Syntax) Assert.Fail();
        }
        catch (InvalidCastException)
        {
            if (type != ErrorType.Semantics) Assert.Fail();
        }
        catch (InvalidOperationException)
        {
            if (type != ErrorType.Semantics) Assert.Fail();
        }
    }


    Dictionary<string, DomainResource> _cache = new Dictionary<string, DomainResource>();

    int numFailed = 0;
    int totalTests = 0;

    [TestMethod, TestCategory("FhirPathFromSpec")]
    public void TestPublishedTests()
    {
        var files = Directory.EnumerateFiles(@"C:\src\fluentpath\tests\dstu2", "*.xml", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            Console.WriteLine("==== Running tests from file '{0}' ====".FormatWith(file));
            runTests(file);
            Console.WriteLine(Environment.NewLine);
        }

        Console.WriteLine("Ran {0} tests in total, {1} succeeded, {2} failed.".FormatWith(totalTests, totalTests - numFailed, numFailed));

        if (numFailed > 0)
        {
            Assert.Fail("There were {0} unsuccessful tests (out of a total of {1})".FormatWith(numFailed, totalTests));
        }

    }

    private void runTests(string pathToTest)
    {
        // Read the test file, then execute each of them
        var doc = XDocument.Load(pathToTest);

        foreach (var item in doc.Descendants("test"))
        {
            string groupName = item.Parent.Attribute("name").Value;
            string name = item.Attribute("name")?.Value ?? "(no name)";
            string inputfile = item.Attribute("inputfile").Value;
            var mode = item.Attribute("mode");
            string expression = item.Element("expression").Value;

            if (mode != null && mode.Value == "strict") continue; // don't do 'strict' tests yet
       
            // Now perform this unit test
            DomainResource resource = null;
            if (!_cache.ContainsKey(inputfile))
            {
                string basepath = @"C:\src\fluentpath\tests\dstu2\input\";
                _cache.Add(inputfile, (DomainResource)(new FhirXmlParser().Parse<DomainResource>(File.ReadAllText(basepath + inputfile))));
            }
            resource = _cache[inputfile];

            try
            {
                totalTests += 1;
                runTestItem(item, resource);
            }
            catch(AssertFailedException afe)
            {
                Console.WriteLine("FAIL: {0} - {1}: {2}", groupName, name, expression);
                Console.WriteLine("   " + afe.Message);
                numFailed += 1;   
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine("FAIL: {0} - {1}: {2}", groupName, name, expression);
                Console.WriteLine("   " + ioe.Message);
                numFailed += 1;
            }
            catch (FormatException fe)
            {
                Console.WriteLine("FAIL: {0} - {1}: {2}", groupName, name, expression);
                Console.WriteLine("   " + fe.Message);
                numFailed += 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("FAIL: {0} - {1}: {2}", groupName, name, expression);
                throw e;
            }
        }
    }

    private void runTestItem(XElement testLine, dstu2::Hl7.Fhir.Model.DomainResource resource)
    {
        var expression = testLine.Element("expression");
        var output = testLine.Elements("output");
        bool hasInvalid;
        string invalid = expression.TryGetAttribute("invalid", out hasInvalid);

        if(hasInvalid)
        {
            ErrorType errorType;

            if (invalid == "syntax")
                errorType = ErrorType.Syntax;
            else if (invalid == "semantic")
                errorType = ErrorType.Semantics;
            else
                throw new ArgumentException("unknown error type");

            testInvalid(resource, errorType, expression.Value);
        }
        else
        {
            // Still need to check the types (and values)
            test(resource, expression.Value, output);
        }
    }

    [TestMethod, TestCategory("FhirPathFromSpec")]
    public void testTyping()
    {
        ElementDefinition ed = new ElementDefinition();
        ed.Binding = new ElementDefinition.BindingComponent();
        ed.Binding.setValueSet(new UriType("http://test.org"));
        testBoolean(null, ed.Binding.getValueSet(), "ElementDefinition.binding.valueSetUri", "startsWith('http:') or startsWith('https') or startsWith('urn:')", true);
    }

    [TestMethod, TestCategory("FhirPathFromSpec")]
    public void testDecimalRA()
    {
        RiskAssessment r = new RiskAssessment();
        SimpleQuantity sq = new SimpleQuantity();
        sq.setValue(0.2);
        sq.setUnit("%");
        sq.setCode("%");
        sq.setSystem("http://unitsofmeasure.org");
        SimpleQuantity sq1 = new SimpleQuantity();
        sq1.setValue(0.4);
        sq1.setUnit("%");
        sq1.setCode("%");
        sq1.setSystem("http://unitsofmeasure.org");
        r.addPrediction().setProbability(new Range().setLow(sq).setHigh(sq1));
        testBoolean(r, r.getPrediction()[0].getProbability(), "RiskAssessment.prediction.probabilityRange",
            "(low.empty() or ((low.code = '%') and (low.system = %ucum))) and (high.empty() or ((high.code = '%') and (high.system = %ucum)))", true);
        testBoolean(r, r.getPrediction()[0], "RiskAssessment.prediction", "probability is decimal implies probability.as(decimal) <= 100", true);
        r.getPrediction()[0].setProbability(new DecimalType(80));
        testBoolean(r, r.getPrediction()[0], "RiskAssessment.prediction", "probability.as(decimal) <= 100", true);
    }

    /*  [TestMethod, TestCategory("FhirPathFromSpec")]
      public void testQuestionnaire()  {
        Questionnaire q = (Questionnaire)FhirParser.ParseResourceFromJson(File.ReadAllText("C:/work/org.hl7.fhir/build - DSTU2.0/publish/questionnaire-example-gcs.json"));
        for (QuestionnaireItemComponent qi : q.getItem()) {
          testQItem(qi);
        }
      }

      private void testQItem(QuestionnaireItemComponent qi)  {
        testBoolean(null, qi, "Questionnaire.item", "(type = 'choice' or type = 'open-choice') or (options.empty() and option.empty())", true);
      }
    */

    [TestMethod, TestCategory("FhirPathFromSpec")]
    public void testExtensionDefinitions()
    {
        Bundle b = new FhirXmlParser().Parse<Bundle>(File.ReadAllText("TestData\\extension-definitions.xml"));
        foreach (Bundle.EntryComponent be in b.Entry)
        {
            testStructureDefinition((StructureDefinition)be.Resource);
        }
    }

    private void testStructureDefinition(StructureDefinition sd)
    {
        testBoolean(sd, sd, "StructureDefinition", "snapshot.element.tail().all(path.startsWith(%resource.snapshot.element.first().path&'.')) and differential.element.tail().all(path.startsWith(%resource.differential.element.first().path&'.'))", true);
    }

}
