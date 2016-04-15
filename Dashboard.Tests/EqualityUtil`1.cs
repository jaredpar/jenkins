// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dashboard.Jenkins.Tests
{
    /// <summary>
    /// Base class which does a lot of the boiler plate work for testing that the equality pattern
    /// is properly implemented in objects
    /// </summary>
    public sealed class EqualityUtil<T>
    {
        private readonly EqualityUnit<T> _equalityUnit;
        private readonly Func<T, T, bool> _compareWithEqualityOperator;
        private readonly Func<T, T, bool> _compareWithInequalityOperator;

        public EqualityUtil(
            EqualityUnit<T> equalityUnit,
            Func<T, T, bool> compEquality = null,
            Func<T, T, bool> compInequality = null)
        {
            _equalityUnit = equalityUnit;
            _compareWithEqualityOperator = compEquality;
            _compareWithInequalityOperator = compInequality;
        }

        public void RunAll(bool checkIEquatable = true)
        {
            if (_compareWithEqualityOperator != null)
            {
                EqualityOperator1();
                EqualityOperator2();
            }

            if (_compareWithInequalityOperator != null)
            {
                InequalityOperator1();
                InequalityOperator2();
            }

            if (checkIEquatable)
            {
                ImplementsIEquatable();
            }

            ObjectEquals1();
            ObjectEquals2();
            ObjectEquals3();
            GetHashCode1();

            if (checkIEquatable)
            {
                EquatableEquals1();
                EquatableEquals2();
            }
        }

        private void EqualityOperator1()
        {
            foreach (var value in _equalityUnit.EqualValues)
            {
                Assert.True(_compareWithEqualityOperator(_equalityUnit.Value, value));
                Assert.True(_compareWithEqualityOperator(value, _equalityUnit.Value));
            }

            foreach (var value in _equalityUnit.NotEqualValues)
            {
                Assert.False(_compareWithEqualityOperator(_equalityUnit.Value, value));
                Assert.False(_compareWithEqualityOperator(value, _equalityUnit.Value));
            }
        }

        private void EqualityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnit.AllValues)
            {
                Assert.False(_compareWithEqualityOperator(default(T), value));
                Assert.False(_compareWithEqualityOperator(value, default(T)));
            }
        }

        private void InequalityOperator1()
        {
            foreach (var value in _equalityUnit.EqualValues)
            {
                Assert.False(_compareWithInequalityOperator(_equalityUnit.Value, value));
                Assert.False(_compareWithInequalityOperator(value, _equalityUnit.Value));
            }

            foreach (var value in _equalityUnit.NotEqualValues)
            {
                Assert.True(_compareWithInequalityOperator(_equalityUnit.Value, value));
                Assert.True(_compareWithInequalityOperator(value, _equalityUnit.Value));
            }
        }

        private void InequalityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnit.AllValues)
            {
                Assert.True(_compareWithInequalityOperator(default(T), value));
                Assert.True(_compareWithInequalityOperator(value, default(T)));
            }
        }

        private void ImplementsIEquatable()
        {
            var type = typeof(T);
            var targetType = typeof(IEquatable<T>);
            Assert.True(type.GetTypeInfo().ImplementedInterfaces.Contains(targetType));
        }

        private void ObjectEquals1()
        {
            var unitValue = _equalityUnit.Value;
            foreach (var value in _equalityUnit.EqualValues)
            {
                Assert.True(value.Equals(unitValue));
                Assert.True(unitValue.Equals(value));
            }
        }

        /// <summary>
        /// Comparison with Null should be false for reference types
        /// </summary>
        private void ObjectEquals2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnit.AllValues)
            {
                Assert.NotNull(value);
            }
        }

        /// <summary>
        /// Passing a value of a different type should just return false
        /// </summary>
        private void ObjectEquals3()
        {
            var allValues = _equalityUnit.AllValues;
            foreach (var value in allValues)
            {
                Assert.False(value.Equals((object)42));
            }
        }

        private void GetHashCode1()
        {
            foreach (var value in _equalityUnit.EqualValues)
            {
                Assert.Equal(value.GetHashCode(), _equalityUnit.Value.GetHashCode());
            }
        }

        private void EquatableEquals1()
        {
            var equatableUnit = (IEquatable<T>)_equalityUnit.Value;
            foreach (var value in _equalityUnit.EqualValues)
            {
                Assert.True(equatableUnit.Equals(value));
                var equatableValue = (IEquatable<T>)value;
                Assert.True(equatableValue.Equals(_equalityUnit.Value));
            }

            foreach (var value in _equalityUnit.NotEqualValues)
            {
                Assert.False(equatableUnit.Equals(value));
                var equatableValue = (IEquatable<T>)value;
                Assert.False(equatableValue.Equals(_equalityUnit.Value));
            }
        }

        /// <summary>
        /// If T is a reference type, null should return false in all cases
        /// </summary>
        private void EquatableEquals2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var cur in _equalityUnit.AllValues)
            {
                var value = (IEquatable<T>)cur;
                Assert.NotNull(value);
            }
        }
    }
}
