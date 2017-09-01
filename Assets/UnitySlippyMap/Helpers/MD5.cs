// 
//  MD5.cs
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
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace UnitySlippyMap
{
	public static class MD5
	{
		public static string GetMD5HashFromFile(string filepath)
		{
			if (filepath == null)
				throw new ArgumentNullException("filepath");
			if (File.Exists(filepath) == false)
				throw new InvalidOperationException("file '" + filepath + "' doesn't exist");
				
			FileStream file = new FileStream(filepath, FileMode.Open);
			System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] bytes = md5.ComputeHash(file);
			file.Close();
			
			return GetMD5HashFromBytes(bytes);
		}
		
		public static string GetMD5HashFromBytes(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException("bytes");
			
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				sb.Append(bytes[i].ToString("x2"));
			}
			return sb.ToString();
		}
	}
}

