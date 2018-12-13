using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

namespace JBooth.VertexPainterPro
{
	public class BakeTexture : IVertexPainterUtility
	{
		public string GetName()
		{
			return "Texture Baker";
		}

		public void OnGUI( PaintJob[] jobs )
		{
			bakingTex = EditorGUILayout.ObjectField( "Texture", bakingTex, typeof( Texture2D ), false ) as Texture2D;

			// Importer
			bakeSourceUV = (BakeSourceUV)EditorGUILayout.EnumPopup( "Source UVs", bakeSourceUV );
			bakeChannel = (BakeChannel)EditorGUILayout.EnumPopup( "Import To", bakeChannel );
			if( bakeSourceUV == BakeSourceUV.WorldSpaceXY || bakeSourceUV == BakeSourceUV.WorldSpaceXZ || bakeSourceUV == BakeSourceUV.WorldSpaceYZ )
			{
				worldSpaceLower = EditorGUILayout.Vector2Field( "Lower world position", worldSpaceLower );
				worldSpaceUpper = EditorGUILayout.Vector2Field( "Upper world position", worldSpaceUpper );
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if( GUILayout.Button( "Import" ) )
			{
				if( bakingTex != null )
				{
					BakeFromTexture( jobs );
				}
				else
				{
					EditorUtility.DisplayDialog( "Error", "Import texture is not set", "ok" );
				}
			}
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			// Exporter
			GUILayout.Space(10);
			brushMode = (VertexPainterWindow.BrushTarget)EditorGUILayout.EnumPopup( "Export From", brushMode);
            padMode = EditorGUILayout.Toggle("Outline Padding", padMode);
			EditorGUILayout.BeginHorizontal();
			exportTexWidth = (TextureSize)EditorGUILayout.EnumPopup( "Width", exportTexWidth );
			exportTexHeight = (TextureSize)EditorGUILayout.EnumPopup( "Height", exportTexHeight );
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if( GUILayout.Button( "Export" ) )
			{
				if (jobs.Length == 1)
					bakingTex = BakeToTexture( jobs, (int)exportTexWidth, (int)exportTexHeight );
				else
					EditorUtility.DisplayDialog( "Error", "Export mesh support only 1.", "ok" );

			}
			GUI.enabled = bakingTex != null;
			if( GUILayout.Button( "Save" ) )
			{
				Texture2D tex = SaveTexture( bakingTex );
				if (tex)
					bakingTex = tex;
			}
			GUI.enabled = true;
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
		}

		public enum BakeChannel
		{
			None,
			Color,
			UV0,
			UV1,
			UV2,
			UV3
		}

		public enum BakeSourceUV
		{
			UV0,
			UV1,
			UV2,
			UV3,
			WorldSpaceXY,
			WorldSpaceXZ,
			WorldSpaceYZ
		}

		Texture2D bakingTex = null;
		BakeSourceUV bakeSourceUV = BakeSourceUV.UV0;
		BakeChannel bakeChannel = BakeChannel.Color;
		Vector2 worldSpaceLower = new Vector2( 0, 0 );
		Vector2 worldSpaceUpper = new Vector2( 1, 1 );

		VertexPainterWindow.BrushTarget brushMode = VertexPainterWindow.BrushTarget.Color;
        bool padMode = false;

		private enum TextureSize
		{
			_16 = 16,
			_32 = 32,
			_64 = 64,
			_128 = 128,
			_256 = 256,
			_512 = 512,
			_1024 = 1024,
			_2048 = 2048,
			_4096 = 4096
		}
		TextureSize exportTexWidth = TextureSize._512;
		TextureSize exportTexHeight = TextureSize._512;

		void InitBakeChannel( BakeChannel bc, PaintJob[] jobs )
		{
			foreach( PaintJob job in jobs )
			{
				if( bc == BakeChannel.Color )
				{
					if( job.stream.colors == null || job.stream.colors.Length != job.verts.Length )
					{
						job.stream.SetColor( Color.black, job.verts.Length );
					}
				}
				else if( bc == BakeChannel.UV0 )
				{
					if( job.stream.uv0 == null || job.stream.uv0.Count != job.verts.Length )
					{
						job.stream.SetUV0( Vector4.zero, job.verts.Length );
					}
				}
				else if( bc == BakeChannel.UV1 )
				{
					if( job.stream.uv1 == null || job.stream.uv1.Count != job.verts.Length )
					{
						job.stream.SetUV1( Vector4.zero, job.verts.Length );
					}
				}
				else if( bc == BakeChannel.UV2 )
				{
					if( job.stream.uv2 == null || job.stream.uv2.Count != job.verts.Length )
					{
						job.stream.SetUV2( Vector4.zero, job.verts.Length );
					}
				}
				else if( bc == BakeChannel.UV3 )
				{
					if( job.stream.uv3 == null || job.stream.uv3.Count != job.verts.Length )
					{
						job.stream.SetUV3( Vector4.zero, job.verts.Length );
					}
				}
				EditorUtility.SetDirty( job.stream );
				EditorUtility.SetDirty( job.stream.gameObject );
			}
		}

		void BakeColor( PaintJob job, BakeChannel bc, Vector4 val, int i )
		{
			switch( bc )
			{
			case BakeChannel.Color:
				{
					job.stream.colors[i] = new Color( val.x, val.y, val.z, val.w );
					break;
				}
			case BakeChannel.UV0:
				{
					job.stream.uv0[i] = val;
					break;
				}
			case BakeChannel.UV1:
				{
					job.stream.uv1[i] = val;
					break;
				}
			case BakeChannel.UV2:
				{
					job.stream.uv2[i] = val;
					break;
				}
			case BakeChannel.UV3:
				{
					job.stream.uv3[i] = val;
					break;
				}
			}
		}

		void BakeFromTexture( PaintJob[] jobs )
		{
			// make sure we have the channels we're baking to..
			InitBakeChannel( bakeChannel, jobs );
			// lets avoid the whole read/write texture thing, because it's lame to require that..
			int w = bakingTex.width;
			int h = bakingTex.height;
			RenderTexture rt = RenderTexture.GetTemporary( w, h, 0, RenderTextureFormat.ARGB32 );
			Graphics.Blit( bakingTex, rt );
			Texture2D tex = new Texture2D( w, h, TextureFormat.ARGB32, false );
			tex.wrapMode = TextureWrapMode.Clamp;
			RenderTexture.active = rt;
			tex.ReadPixels( new Rect( 0, 0, w, h ), 0, 0 );
			foreach( PaintJob job in jobs )
			{
				List<Vector4> srcUV0 = new List<Vector4>();
				List<Vector4> srcUV1 = new List<Vector4>();
				List<Vector4> srcUV2 = new List<Vector4>();
				List<Vector4> srcUV3 = new List<Vector4>();
				job.meshFilter.sharedMesh.GetUVs( 0, srcUV0 );
				job.meshFilter.sharedMesh.GetUVs( 1, srcUV1 );
				job.meshFilter.sharedMesh.GetUVs( 2, srcUV2 );
				job.meshFilter.sharedMesh.GetUVs( 3, srcUV3 );
				for( int i = 0; i < job.verts.Length; ++i )
				{
					Vector4 uv = Vector4.zero;

					switch( bakeSourceUV )
					{
					case BakeSourceUV.UV0:
						{
							if( job.stream.uv0 != null && job.stream.uv0.Count == job.verts.Length )
								uv = job.stream.uv0[i];
							else if( srcUV0 != null && srcUV0.Count == job.verts.Length )
								uv = srcUV0[i];
							break;
						}
					case BakeSourceUV.UV1:
						{
							if( job.stream.uv1 != null && job.stream.uv1.Count == job.verts.Length )
								uv = job.stream.uv1[i];
							else if( srcUV1 != null && srcUV1.Count == job.verts.Length )
								uv = srcUV1[i];
							break;
						}
					case BakeSourceUV.UV2:
						{
							if( job.stream.uv2 != null && job.stream.uv2.Count == job.verts.Length )
								uv = job.stream.uv2[i];
							else if( srcUV2 != null && srcUV2.Count == job.verts.Length )
								uv = srcUV2[i];
							break;
						}
					case BakeSourceUV.UV3:
						{
							if( job.stream.uv3 != null && job.stream.uv3.Count == job.verts.Length )
								uv = job.stream.uv3[i];
							else if( srcUV3 != null && srcUV3.Count == job.verts.Length )
								uv = srcUV3[i];
							break;
						}
					case BakeSourceUV.WorldSpaceXY:
						{
							Vector3 pos = job.stream.transform.localToWorldMatrix.MultiplyPoint( job.GetPosition( i ) );
							Vector2 p = new Vector2( pos.x, pos.y ) - worldSpaceLower;
							Vector2 scale = worldSpaceUpper - worldSpaceLower;
							scale.x = Mathf.Max( 0.000001f, scale.x );
							scale.y = Mathf.Max( 0.000001f, scale.y );
							uv = p;
							uv.x /= scale.x;
							uv.y /= scale.y;
							break;
						}
					case BakeSourceUV.WorldSpaceXZ:
						{
							Vector3 pos = job.stream.transform.localToWorldMatrix.MultiplyPoint( job.GetPosition( i ) );
							Vector2 p = new Vector2( pos.x, pos.z ) - worldSpaceLower;
							Vector2 scale = worldSpaceUpper - worldSpaceLower;
							scale.x = Mathf.Max( 0.000001f, scale.x );
							scale.y = Mathf.Max( 0.000001f, scale.y );
							uv = p;
							uv.x /= scale.x;
							uv.y /= scale.y;
							break;
						}
					case BakeSourceUV.WorldSpaceYZ:
						{
							Vector3 pos = job.stream.transform.localToWorldMatrix.MultiplyPoint( job.GetPosition( i ) );
							Vector2 p = new Vector2( pos.y, pos.z ) - worldSpaceLower;
							Vector2 scale = worldSpaceUpper - worldSpaceLower;
							scale.x = Mathf.Max( 0.000001f, scale.x );
							scale.y = Mathf.Max( 0.000001f, scale.y );
							uv = p;
							uv.x /= scale.x;
							uv.y /= scale.y;
							break;
						}
					}
					Color c = tex.GetPixel( (int)( uv.x * w ), (int)( uv.y * w ) );

					BakeColor( job, bakeChannel, new Vector4( c.r, c.g, c.b, c.a ), i );

				}
				job.stream.Apply();
			}
		}

		Texture2D BakeToTexture( PaintJob[] jobs, int w, int h )
		{
			RenderTexture rt = RenderTexture.GetTemporary( w, h, 0, RenderTextureFormat.ARGB32 );
			Texture2D tex = new Texture2D( w, h, TextureFormat.ARGB32, false );
			tex.wrapMode = TextureWrapMode.Clamp;
			RenderTexture.active = rt;
			
			Material m = new Material( Shader.Find( "Hidden/VertexPainterPro_Preview" ) );
			m.SetInt( "_flowVisualization", (int)0 );
			m.SetInt( "_tab", (int)0 );
			m.SetInt( "_flowTarget", (int)0 );
			m.SetInt( "_channel", (int)brushMode );
			m.SetVector( "_uvRange", new Vector2( 0, 1 ) );

			var CamObj = new GameObject("BakeTexture Camera");
			CamObj.transform.position = new Vector3( 0, 0, -1 );
			var camera = CamObj.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = Color.black;
			camera.orthographic = true;
			camera.orthographicSize = 1;
			camera.cullingMask = 1<<7;
			camera.targetTexture = rt;
            camera.allowMSAA = false;

            foreach ( PaintJob job in jobs )
			{
				Mesh source = job.meshFilter.sharedMesh;
				Mesh mesh = Object.Instantiate( source );

				List<Vector3> srcVertices = new List<Vector3>();
				source.GetVertices( srcVertices );
				
				List<Vector4> srcUV0 = new List<Vector4>();
				source.GetUVs( 0, srcUV0 );

				List<Color> srcColors = new List<Color>();
				source.GetColors( srcColors );

				for( int i = 0; i < job.verts.Length; ++i )
				{
					// Vertex
					Vector4 uv = Vector4.zero;
					if( job.stream.uv0 != null && job.stream.uv0.Count == job.verts.Length )
						uv = job.stream.uv0[i];
					else if( srcUV0 != null && srcUV0.Count == job.verts.Length )
						uv = srcUV0[i];
					uv *= 2;
					uv -= Vector4.one;

					srcVertices[i] = new Vector3( uv.x, uv.y, 0 );

					// Color
					if( job.stream.colors != null && job.stream.colors.Length == job.verts.Length )
						srcColors[i] = job.stream.colors[i];
				}

				mesh.SetVertices( srcVertices );
				mesh.SetColors( srcColors );
				var bound = mesh.bounds;
				bound.center = Vector3.zero;
				bound.size = Vector3.one;
				mesh.bounds = bound;
                Graphics.DrawMesh( mesh, Matrix4x4.identity, m, 7);
				camera.Render();
			}

			tex.ReadPixels( new Rect( 0, 0, w, h ), 0, 0 );
			tex.Apply();

            if (padMode)
            {
                var pad = new TexturePadding(tex);
                pad.Padding((w + h) / 50);
            }

			RenderTexture.active = null;
			camera.targetTexture = null;
			Object.DestroyImmediate(CamObj);
			Object.DestroyImmediate(m);
			RenderTexture.ReleaseTemporary( rt );
			return tex;
		}

        class TexturePadding
        {
            public Texture2D texture;
            Color32[] newcolors;
            Color32[] colors;
            int w, h;
            public TexturePadding(Texture2D tex)
            {
                texture = tex;
                w = tex.width;
                h = tex.height;

                colors = tex.GetPixels32();
                newcolors = new Color32[colors.Length];
            }

            static int Clamp(int x, int width)
            {
                return x < 0 ? 0 : (x < width - 1 ? x : width - 1);
            }
            Color32 GetColor(int x, int y)
            {
                x = Clamp(x, w);
                y = Clamp(y, h);
                return colors[x + y * w];
            }
            void SetColor(int x, int y, Color32 c)
            {
                newcolors[x + y * w] = c;
            }

            static bool IsBlack(Color32 c)
            {
                return c.r == 0 && c.g == 0 && c.b == 0;
            }
            bool IsBlack(int x, int y)
            {
                return IsBlack(GetColor(x, y));
            }
            Color32 GetNeighborColor(int x, int y)
            {
                Color output = Color.black;
                float count = 0;
                Color32 c;

                c = GetColor(x + 1, y - 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x + 1, y);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x + 1, y + 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x, y - 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x, y + 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x - 1, y - 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x - 1, y);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = GetColor(x - 1, y + 1);
                if (!IsBlack(c))
                {   output += c; count += 1; }

                c = output / count;
                c.a = 255;
                return c;
            }

            void PaddingOnce()
            {
                for (int j = 0; j < texture.height; j++)
                {
                    for (int i = 0; i < texture.width; i++)
                    {
                        Color32 c = GetColor(i, j);
                        if (IsBlack(c))
                            c = GetNeighborColor(i, j);
                        SetColor(i, j, c);
                    }
                }
            }
            public void Padding(int count)
            {
                for (int pad = 0; pad < count; pad++)
                {
                    PaddingOnce();
                    newcolors.CopyTo(colors, 0);
                }

                texture.SetPixels32(colors);
                texture.Apply();
            }
        }

		private Texture2D SaveTexture( Texture2D texture )
		{
            string path = EditorUtility.SaveFilePanel("Save Texture", Application.dataPath, "texture", "png");
			if( !string.IsNullOrEmpty( path ) )
			{
				path = FileUtil.GetProjectRelativePath(path);

				byte[] data = texture.EncodeToPNG();
				File.WriteAllBytes( path, data );
				AssetDatabase.Refresh();

				var asset = AssetDatabase.LoadAssetAtPath( path, typeof( Texture2D ) ) as Texture2D;
				return asset;
			}
			return null;
		}
	}
}
