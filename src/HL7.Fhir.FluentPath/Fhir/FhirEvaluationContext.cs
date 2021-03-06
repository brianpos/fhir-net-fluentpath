﻿/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */


using Hl7.Fhir.FluentPath.InstanceTree;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hl7.Fhir.FluentPath
{
#if MOVED_TO_FHIR_ASSEMBLY
    public class FhirEvaluationContext : BaseEvaluationContext
    {
        public FhirEvaluationContext()
        {
        }

        public FhirEvaluationContext(FhirClient client) : this(client, null)
        {
        }

        public FhirEvaluationContext(IFluentPathElement originalResource) : this(null, originalResource)
        {
        }

        public FhirEvaluationContext(FhirClient client, IFluentPathElement originalResource)
        {
            FhirClient = client;
            OriginalResource = originalResource;
        }


        public IEnumerable<IFluentPathValue> OriginalContext { get; set; }

        public IFluentPathElement OriginalResource { get; set; }

        FhirClient FhirClient { get; set; }

        public virtual void InvokeExternalFunction(string name, IList<IEnumerable<IFluentPathValue>> parameters)
        {
            throw new NotSupportedException("Function '{0}' is unknown".FormatWith(name));
        }

        public virtual void Log(string argument, IEnumerable<IFluentPathValue> focus)
        {
            System.Diagnostics.Trace.WriteLine(argument);

            foreach (var element in focus)
            {
                System.Diagnostics.Trace.WriteLine("=========");
                System.Diagnostics.Trace.WriteLine(element.ToString());
            }
        }

        public override IFluentPathValue ResolveConstant(string name)
        {
            string value = null;

            if (name.StartsWith("ext-"))
                value = "http://hl7.org/fhir/StructureDefinition/" + name.Substring(4);
            else if (name.StartsWith("vs-"))
                value = "http://hl7.org/fhir/ValueSet/" + name.Substring(3);
            else if (name == "sct")
                value = "http://snomed.info/sct";
            else if (name == "loinc")
                value = "http://loinc.org";
            else if (name == "ucum")
                value = "http://unitsofmeasure.org";

            return value != null ? new TypedValue(value) : null;
        }

        public virtual IFluentPathElement ResolveResource(string url)
        {
            if (FhirClient == null)
                throw Error.InvalidOperation("The EvaluationContext does not have a FhirClient to use to resolve url '{0}'".FormatWith(url));

            try
            {
                var resource = FhirClient.Get(url);
                if (resource == null) return null;

                var xml = FhirSerializer.SerializeResourceToXml(resource);
                return TreeConstructor.FromXml(xml);                
            }
            catch(Exception e)
            {
                throw e;
                //return null;
            }
        }
    }
#endif
}
