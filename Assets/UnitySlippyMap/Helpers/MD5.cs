// 
//  MD5.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

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

