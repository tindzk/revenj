﻿// This software is part of the Autofac IoC container
// Copyright © 2011 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Revenj.Extensibility.Autofac.Util
{
	/// <summary>
	/// Helper methods used throughout the codebase.
	/// </summary>
	static class Enforce
	{
		/// <summary>
		/// Enforce that an argument is not null. Returns the
		/// value if valid so that it can be used inline in
		/// base initialiser syntax.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="name"></param>
		/// <returns><paramref name="value"/></returns>
		public static T ArgumentNotNull<T>(T value, string name)
			where T : class
		{
			if (value == null)
				throw new ArgumentNullException(name);

			return value;
		}

		/// <summary>
		/// Enforce that sequence does not contain null. Returns the
		/// value if valid so that it can be used inline in
		/// base initialiser syntax.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">The value.</param>
		/// <param name="name">The name.</param>
		/// <returns><paramref name="value"/></returns>
		public static T ArgumentElementNotNull<T>(T value, string name)
			where T : class, IEnumerable
		{
			if (value == null) throw new ArgumentNullException(name);

			// Contains(null) does not work on Mono, must use Any(...)
			if (value.Cast<object>().Any(v => v == null))
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "The sequence provided as argument '{0}' cannot contain null elements.", name));

			return value;
		}

		/// <summary>
		/// Enforces that the provided object is non-null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">The value.</param>
		/// <returns><paramref name="value"/></returns>
		public static T NotNull<T>(T value)
			where T : class
		{
			if (value == null)
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
					"The object of type '{0}' cannot be null.", typeof(T).FullName));

			return value;
		}

		/// <summary>
		/// Enforce that an argument is not null or empty. Returns the
		/// value if valid so that it can be used inline in
		/// base initialiser syntax.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="description">The description.</param>
		/// <returns><paramref name="value"/></returns>
		public static string ArgumentNotNullOrEmpty(string value, string description)
		{
			if (description == null) throw new ArgumentNullException("description");
			if (value == null) throw new ArgumentNullException(description);

			if (value == string.Empty)
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
					"The argument '{0}' cannot be empty.", description));

			return value;
		}

		/// <summary>
		/// Enforce that the argument is a delegate type.
		/// </summary>
		/// <param name="delegateType">The type to test.</param>
		public static void ArgumentTypeIsFunction(Type delegateType)
		{
			if (delegateType == null) throw new ArgumentNullException("delegateType");

			MethodInfo invoke = delegateType.GetMethod("Invoke");
			if (invoke == null)
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
					"Type {0} is not a delegate type.", delegateType));
			else if (invoke.ReturnType == typeof(void))
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
					"Type {0} returns void.", delegateType));
		}
	}
}
