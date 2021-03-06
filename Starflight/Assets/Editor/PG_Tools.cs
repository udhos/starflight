﻿
using UnityEngine;
using System.IO;
using System.Threading;

class PG_Tools
{
	// thread safe floating point add
	public static float ThreadSafeAdd( ref float location, float value )
	{
		float newCurrentValue = location;

		while ( true )
		{
			float currentValue = newCurrentValue;

			float newValue = currentValue + value;

			newCurrentValue = Interlocked.CompareExchange( ref location, newValue, currentValue );

			if ( newCurrentValue == currentValue )
			{
				return newValue;
			}
		}
	}

	// saves a texture map to file as a png image
	public static void SaveAsPNG( Texture2D textureMap, string filename )
	{
		var data = textureMap.EncodeToPNG();

		var directory = Path.GetDirectoryName( filename );

		Directory.CreateDirectory( directory );

		File.WriteAllBytes( filename, data );
	}

	// saves a texture map to file as an exr images
	public static void SaveAsEXR( Texture2D textureMap, string filename )
	{
		var data = textureMap.EncodeToEXR();

		var directory = Path.GetDirectoryName( filename );

		Directory.CreateDirectory( directory );

		File.WriteAllBytes( filename, data );
	}

	// saves a float array to file as png images
	public static void SaveAsPNG( float[,] alpha, string filename )
	{
		var width = alpha.GetLength( 1 );
		var height = alpha.GetLength( 0 );

		var textureMap = new Texture2D( width, height, TextureFormat.RGB24, false );

		for ( var y = 0; y < height; y++ )
		{
			for ( var x = 0; x < width; x++ )
			{
				float a = alpha[ y, x ];

				textureMap.SetPixel( x, y, new Color( a, a, a, 1.0f ) );
			}
		}

		textureMap.Apply();

		SaveAsPNG( textureMap, filename );
	}

	// saves a color array to file as a png image
	public static void SaveAsPNG( Color[,] colors, string filename )
	{
		var width = colors.GetLength( 1 );
		var height = colors.GetLength( 0 );

		var textureMap = new Texture2D( width, height, TextureFormat.RGB24, false );

		for ( var y = 0; y < height; y++ )
		{
			for ( var x = 0; x < width; x++ )
			{
				textureMap.SetPixel( x, y, new Color( colors[ y, x ].r, colors[ y, x ].g, colors[ y, x ].b, 1.0f ) );
			}
		}

		textureMap.Apply();

		SaveAsPNG( textureMap, filename );
	}

	// saves a color array to file as an exr image
	public static void SaveAsEXR( Color[,] colors, string filename )
	{
		var width = colors.GetLength( 1 );
		var height = colors.GetLength( 0 );

		var textureMap = new Texture2D( width, height, TextureFormat.RGBAFloat, false );

		for ( var y = 0; y < height; y++ )
		{
			for ( var x = 0; x < width; x++ )
			{
				textureMap.SetPixel( x, y, new Color( colors[ y, x ].r, colors[ y, x ].g, colors[ y, x ].b, colors[ y, x ].a ) );
			}
		}

		textureMap.Apply();

		SaveAsEXR( textureMap, filename );
	}
}
