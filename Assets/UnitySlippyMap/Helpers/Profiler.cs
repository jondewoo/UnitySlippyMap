// 
//  Profiler.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
// Copyright (c) 2017 Jonathan Derrough
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace UnitySlippyMap
{
	// <summary>
	// A simple profiler class
	// </summary>
	// <example>
	// Profiler.Begin("foo");
	// [do stuff]
	// Profiler.Begin("bar");
	// [do some more stuff]
	// Profiler.End("bar");
	// [do stuff one more]
	// Profiler.End("foo");
	// Debug.Log(Profiler.Dump());
	// </example>
	public static class Profiler
	{
		private class Profile
		{
			public string Name = String.Empty;
			public int Count = 0;
			public Stopwatch Watch = new Stopwatch();
			public TimeSpan Ts = new TimeSpan(0L);
			public Dictionary<string, Profile> Children = new Dictionary<string, Profile>();
			public Profile Parent = null;
			
			public Profile(string name)
			{
				this.Name = name;
			}
		}
		
		private static Profile root = new Profile("Root");
		private static Profile current = root;
		
		public static void Begin(string name)
		{
			Profile profile = null;
			if (!current.Children.TryGetValue(name, out profile))
			{
				profile = new Profile(name);
				profile.Parent = current;
				current.Children.Add(name, profile);
			}
			
			++profile.Count;
			current = profile;
			
			current.Watch.Reset();
			current.Watch.Start();
			
			if (!root.Watch.IsRunning)
			{
				root.Watch.Reset();
				root.Watch.Start();
			}
		}
		
		public static void End(string name)
		{
			if (current.Name != name)
			{
				throw new InvalidOperationException("Mismatched Profiler.End(" + name + ") call.");
			}
			
			current.Watch.Stop();
			current.Ts += current.Watch.Elapsed;
			
			current = current.Parent;
			
			if (current.Parent == null)
			{
				root.Watch.Stop();
				root.Ts += root.Watch.Elapsed;
			}
		}
		
		private static void Dump(Profile profile, string indentation, out string text)
		{
			int percentage = 100;
			if (profile.Parent != null)
			{
				try
				{
					if (profile.Parent.Ts.Ticks != 0)
						percentage = (int)Math.Round((double)profile.Ts.Ticks / (double)profile.Parent.Ts.Ticks * 100.0);
					else
						percentage = 0;
				}
				catch (OverflowException e)
				{
					UnityEngine.Debug.LogError(e.Message + ": profile.Ts.Ticks: " + profile.Ts.Ticks
					                           + ": profile.Parent.Ts.Ticks: " + profile.Parent.Ts.Ticks);
				}
			}
			text = String.Format("{0}[{1}% | {2} calls] {3}: {4:00}:{5:00}:{6:00}.{7:00}\n",
			                     indentation,
			                     percentage,
			                     profile.Count,
			                     profile.Name,
			                     profile.Ts.Hours,
			                     profile.Ts.Minutes,
			                     profile.Ts.Seconds,
			                     profile.Ts.Milliseconds / 10);
			
			indentation += "\t";
			
			foreach (KeyValuePair<string, Profile> child in profile.Children)
			{
				string childText;
				Dump(child.Value, indentation, out childText);
				text += childText;
			}
		}
		
		public static string Dump()
		{
			string result = String.Empty;
			
			Dump(root, "", out result);
			return result;
		}
		
		public static void Reset()
		{
			Reset(root);
		}
		
		private static void Reset(Profile p)
		{
			p.Count = 0;
			p.Watch.Reset();
			p.Ts = new TimeSpan(0);
			foreach (KeyValuePair<string, Profile> child in p.Children)
			{
				Reset(child.Value);
			}
		}
	}
}