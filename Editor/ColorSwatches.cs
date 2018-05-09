using UnityEngine;
using System.Collections;

namespace JBooth.VertexPainterPro
{
   [System.Serializable]
   class ColorSwatches : ScriptableObject
   {
      public Color[] colors = new Color[] { Color.white, Color.black, Color.red, Color.green, Color.blue, Color.cyan,
         Color.magenta, Color.yellow,
		 Color.white * 0.1f,
		 Color.white * 0.2f,
		 Color.white * 0.3f,
		 Color.white * 0.4f,
		 Color.white * 0.5f,
		 Color.white * 0.6f,
		 Color.white * 0.7f,
		 Color.white * 0.8f,
		 Color.white * 0.9f
      };
   }
}