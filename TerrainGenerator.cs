using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainGenerator : MonoBehaviour {
	public Terrain terrain;
	public Texture2D heightmap;
	public Texture2D heightmapDebugger;

	[Range(0,100)]
	public int shoreDepth = 5; //percentual
	[Range(0f,0.4f)]
	public float maxTerrainHeight = 1;
	[Range(0,0.4f)]
	public float minTerrainHeight = 0.25f;
	public float terrainScale = 10;
	public float heightmapDetail = 3;
	[Range(0f,1f)]
	public float terrainSharpness = 1;

	public float textureNoiseScale = 10;
	[Range(0f,1f)]
	public float grassAmount = 1; //grass only grows on flats
	[Range(0f,1f)]
	public float dirtAmount = 1; //dirt only grows on flats
	[Range(0f,1f)]
	public float mossAmount = 1; //moss grows on flats and slopes
	[Range(0f,0.4f)]
	public float sandheight = 0.2f;

	private const int MOSS	    = 2; 
	private const int GRASS     = 1;
	private const int DIRT	    = 3; 
	private const int SAND      = 0;
	private const int STONE     = 4;

	// Use this for initialization
	void Start () {
		InitializeHeightmap ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	[ContextMenu("Generate Random Relief")]
	void InitializeHeightmap() {
		heightmap = GenerateHeightmap (
			terrain.terrainData.heightmapWidth, 
			terrain.terrainData.heightmapHeight);
		GenerateRelief (heightmap);
		heightmap.Apply ();
		PaintTextures ();
	}

	Texture2D GenerateHeightmap(int width, int height) {
		float seed = Random.Range (0, 10000);
		float shoreWidth = width / (100 / shoreDepth);
		float shoreHeight = height / (100 / shoreDepth);
		Texture2D texture = new Texture2D (width, height);
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				float xCoord = (seed+(float)x) / (float)width * terrainScale;
				float yCoord = (seed+(float)y) / (float)height * terrainScale;
				float color = Mathf.Max (
					minTerrainHeight, 
					Mathf.PerlinNoise(xCoord, yCoord)*maxTerrainHeight);

				if (x < shoreWidth)
					color = color * Remap (x, 0, shoreWidth, 0, 1);
				if (y < shoreHeight)
					color = color * Remap (y, 0, shoreHeight, 0, 1);
				if (x > width - shoreWidth)
					color = color * Remap (x, width - shoreWidth, width, 1, 0);
				if (y > height - shoreHeight)
					color = color * Remap (y, height - shoreHeight, height, 1, 0);
				
				texture.SetPixel (x, y, new Color (color, color, color));
			}
		}
		if (terrainSharpness < 1) {
			TextureScale.Bilinear (texture, (int)(width * terrainSharpness), (int)(height * terrainSharpness));
			TextureScale.Bilinear (texture, width, height);
		}
		return texture;
	}

	[ContextMenu("Update debug heightmap")]
	void UpdateDebugHeightmap() {
		StartCoroutine (UpdateDebugHeightmap (heightmap));
	}

	private IEnumerator UpdateDebugHeightmap(Texture2D texture) {
		print ("hi");
		heightmapDebugger.Resize (texture.width, texture.height);
		print ("hi");
		for (int x = 0; x < texture.width; x++) {
			for (int y = 0; y < texture.height; y++) {
				heightmapDebugger.SetPixel (x, y, texture.GetPixel (x, y));
			}
		}
		print ("hi");
		heightmapDebugger.Apply ();
		yield return null;
	}
		
	void GenerateRelief(Texture2D heightmap) {
		int width = terrain.terrainData.heightmapWidth;
		int height = terrain.terrainData.heightmapHeight;
		if (heightmap.width != width && heightmap.height != height)
			heightmap.Resize (width, height);
		terrain.terrainData.SetHeights (0, 0, TextureToFloatArray (heightmap));
	}

	float[,] TextureToFloatArray(Texture2D texture) {
		float[,] array = new float[texture.width, texture.height];
		for (int x = 0; x < texture.width; x++) {
			for (int y = 0; y < texture.height; y++) {
				array [x, y] = texture.GetPixel (x, y).grayscale;
			}
		}
		return array;
	}
		
	[ContextMenu("Paint Textures")]
	void PaintTextures() {
		int width = terrain.terrainData.alphamapWidth;
		int height = terrain.terrainData.alphamapHeight;
		float[,,] alphaData = terrain.terrainData.GetAlphamaps(0, 0, width, height);

		TextureScale.Bilinear (heightmap, width, height);
		Texture2D grassNoise = Noise (width, height, textureNoiseScale, 0, grassAmount);
		Texture2D mossNoise = Noise (width, height, textureNoiseScale, 0, grassAmount);
		Texture2D dirtNoise = Noise (width, height, textureNoiseScale, 0, dirtAmount);
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				alphaData [x, y, GRASS] = grassNoise.GetPixel (x, y).grayscale;
				alphaData [x, y, MOSS] = mossNoise.GetPixel (x, y).grayscale;
				alphaData [x, y, DIRT] = dirtNoise.GetPixel (x, y).grayscale;
				alphaData [x, y, STONE] = 1f;

				float hm = heightmap.GetPixel (x, y).grayscale;

				if (hm < sandheight / 2) {
					alphaData [x, y, SAND] = 1;
					alphaData [x, y, GRASS] = 0;
					alphaData [x, y, MOSS] = 0;
					alphaData [x, y, DIRT] = 0;
				} else if (hm < sandheight) {
					alphaData [x, y, SAND] = 1 - Remap (hm, sandheight / 2, sandheight, 0, 1);
				} else {
					alphaData [x, y, SAND] = 0;
				}
			}
		}
		terrain.terrainData.SetAlphamaps (0, 0, alphaData);
	}

	Texture2D Noise (int width, int height, float noiseScale, float ground, float intensity) {
		Texture2D texture = new Texture2D (width, height);
		float seed = Random.Range (0, 10000);
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				float xCoord = (seed+(float)x) / (float)width * noiseScale;
				float yCoord = (seed+(float)y) / (float)height * noiseScale;
				float color = Mathf.Max (
					ground, 
					Mathf.PerlinNoise(xCoord, yCoord)*intensity);
				texture.SetPixel (x, y, new Color (color, color, color));
			}
		}
		texture.Apply ();
		return texture;
	}

	float Remap (float value, float low1, float high1, float low2, float high2) {
		return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
	}


}
