using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Extracts production sprites from the approved generated atlases without resampling.</summary>
public static class CompleteGameArtPreparation
{
    private const string Folder = "Assets/ArtRes/WorldPolished/";
    [MenuItem("Tools/TapGJ/Complete Game/Prepare World Sprites")]
    public static void Prepare()
    {
        Extract("PlatformTiles.png", new[] {
            new Slice("rock_fill",79,212,469,439,false),
            new Slice("rock_top_moss",655,430,524,225,true),
            new Slice("metal_deck",0,700,627,554,true),
            new Slice("pipe_horizontal",627,700,627,554,true)
        });
        Extract("WorldProps-source.png", new[] {
            new Slice("crystal",0,0,512,476,true),
            new Slice("mushroom",512,0,512,512,true),
            new Slice("pipe_elbow",1024,0,512,512,true),
            new Slice("gene_terminal",0,476,512,548,true),
            new Slice("supply_crate",530,512,465,512,true),
            new Slice("core_gate",995,512,541,512,true)
        });
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("World artwork prepared: ten individually bounded sprites.");
    }
    private struct Slice
    {
        public string name; public int x,y,w,h; public bool trim;
        public Slice(string name,int x,int y,int w,int h,bool trim)
        {this.name=name;this.x=x;this.y=y;this.w=w;this.h=h;this.trim=trim;}
    }
    private static void Extract(string filename,Slice[] slices)
    {
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            ImageConversion.LoadImage(source,File.ReadAllBytes(Folder+filename));
            Color32[] pixels=source.GetPixels32();
            foreach(Slice slice in slices)
            {
                int x0=slice.x,x1=slice.x+slice.w-1,y0=source.height-slice.y-slice.h,y1=source.height-slice.y-1;
                if(x0<0||y0<0||x1>=source.width||y1>=source.height)throw new InvalidOperationException("Atlas bounds changed: "+slice.name);
                int minX=x0,maxX=x1,minY=y0,maxY=y1;
                if(slice.trim)
                {
                    minX=x1;maxX=x0;minY=y1;maxY=y0;
                    for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                        if(pixels[y*source.width+x].a>64)
                        {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                }
                int width=maxX-minX+1,height=maxY-minY+1;
                var output=new Texture2D(width,height,TextureFormat.RGBA32,false);
                try
                {
                    var data=new Color32[width*height];
                    for(int y=0;y<height;y++)Array.Copy(pixels,(minY+y)*source.width+minX,data,y*width,width);
                    output.SetPixels32(data);output.Apply();File.WriteAllBytes(Folder+slice.name+".png",output.EncodeToPNG());
                }
                finally{UnityEngine.Object.DestroyImmediate(output);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(source);}
    }
}
