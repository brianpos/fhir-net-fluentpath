﻿/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hl7.Fhir.FluentPath.InstanceTree
{
    /// <summary>Represents a FHIR navigation tree with node values of type <see cref="IFluentPathElement"/>.</summary>
    public class FhirInstanceTree : ValueNavigationTree<FhirInstanceTree, IFluentPathValue>, IFluentPathElement
    {
        #region Public Factory Methods

        /// <summary>Create a new <see cref="FhirInstanceTree"/> root node with the specified name.</summary>
        /// <param name="name">The name of the new node.</param>
        /// <returns>A new <see cref="FhirInstanceTree"/> node.</returns>
        public static FhirInstanceTree Create(string name) { return new FhirInstanceTree(null, null, name, null); }

        /// <summary>Create a new <see cref="FhirInstanceTree"/> node with the specified name and value.</summary>
        /// <param name="name">The name of the new node.</param>
        /// <param name="value">The node value.</param>
        /// <returns>A new <see cref="FhirInstanceTree"/> node.</returns>
        public static FhirInstanceTree Create(string name, IFluentPathValue value) { return new FhirInstanceTree(null, null, name, value); }

        #endregion

        protected FhirInstanceTree(FhirInstanceTree parent, FhirInstanceTree previousSibling, string name, IFluentPathValue value) : base(parent, previousSibling, name, value) { }

        protected override FhirInstanceTree Self { get { return this; } }

        protected override FhirInstanceTree CreateNode(FhirInstanceTree parent, FhirInstanceTree previousSibling, string name)
        {
            return new FhirInstanceTree(parent, previousSibling, name, null);
        }

        protected override FhirInstanceTree CreateNode(FhirInstanceTree parent, FhirInstanceTree previousSibling, string name, IFluentPathValue value)
        {
            return new FhirInstanceTree(parent, previousSibling, name, value);
        }

        object IFluentPathValue.Value
        {
            get
            {
                if (Self.Value != null)
                    return Self.Value.Value;
                else
                    return null;
            }
        }

        IEnumerable<ChildNode> IFluentPathElement.Children()
        {
            return LinkedTreeExtensions.Children(this).Select(c => new ChildNode(c.Name,c));
        }

        IFluentPathElement IFluentPathElement.Parent
        {
            get { return Parent; }
        }

    }
}
