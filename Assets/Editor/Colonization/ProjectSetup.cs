using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Colonization.Editor
{
    public static class ProjectSetup
    {
        private const string ArtRoot = "Assets/Art/Colonization/";
        private const string DataRoot = "Assets/Data/Colonization/";
        private const string PrefabRoot = "Assets/Prefabs/Colonization/";
        public const string ScenePath = "Assets/Scenes/Map01.unity";
        [Serializable] private sealed class NationJson
        {
            public string name, leader, date, region, bio, note, enemy, aggressive;
            public string[] terrain, goods, jobs, sources;
        }
        [MenuItem("Colonization/Import artwork and build Map01")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(DataRoot); Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory("Docs");
            AssetDatabase.Refresh();
            ImportArt();
            GameCatalog catalog = Catalog();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PrefabRoot + "WorldSprite.mat");
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, PrefabRoot + "WorldSprite.mat"); }
            Hex hexPrefab = HexPrefab(material);
            Unit unitPrefab = UnitPrefab(material);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController));
            cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(42,18,-10);
            Camera camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 9;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.065f,.085f); camera.nearClipPlane=.1f;camera.farClipPlane=100;
            var game = new GameObject("GameManager",typeof(GameManager)).GetComponent<GameManager>();
            var view = new GameObject("UIManager",typeof(GameUI)).GetComponent<GameUI>();
            game.Configure(catalog,hexPrefab,unitPrefab,cameraObject.GetComponent<CameraController>(),view);
            if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Map01 could not be saved.");
            // Map01 becomes the playable entry scene. Retain all existing scenes after it.
            var scenes = new List<EditorBuildSettingsScene> {new EditorBuildSettingsScene(ScenePath,true)};
            scenes.AddRange(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
            Debug.Log("COLONIZATION_SETUP_OK: 68 PNGs, 11 terrain data, 20 land units, 6 ships, 8 Native nations; Map01 saved.");
        }
        private static void ImportArt()
        {
            foreach(string path in Directory.GetFiles(ArtRoot,"*",SearchOption.AllDirectories))
            {
                string extension=Path.GetExtension(path).ToLowerInvariant();
                if(extension!=".png" && extension!=".jpg") continue;
                var importer=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;
                if(importer==null) throw new InvalidOperationException("Texture importer missing: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        private static T Data<T>(string name) where T:ScriptableObject
        {
            string path=DataRoot+name+".asset";
            T value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}
            EditorUtility.SetDirty(value);return value;
        }
        private static Sprite Sprite(string path)
        {
            Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null) throw new InvalidOperationException("Missing sprite "+path);
            return sprite;
        }
        private static ArtSprite Art(string path)
        {
            Sprite sprite=Sprite(path);
            // Read original alpha to place visible artwork, rather than treating its transparent padding as part of the tile.
            // Original image bytes and sprite rectangles remain unchanged.
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot decode "+path);
            Color32[] pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
                if(pixels[y*texture.width+x].a>16){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(maxX<minX) throw new InvalidOperationException("Empty transparent artwork: "+path);
            var result=new ArtSprite {sprite=sprite,
                visibleCenter=new Vector2((minX+maxX+1)*.5f-texture.width*.5f,(minY+maxY+1)*.5f-texture.height*.5f)/100,
                visibleSize=new Vector2(maxX-minX+1,maxY-minY+1)/100};
            UnityEngine.Object.DestroyImmediate(texture);return result;
        }
        private static string Find(string directory,string prefix) => Directory.GetFiles(ArtRoot+directory,prefix+"*.png").Single().Replace('\\','/');
        private static GameCatalog Catalog()
        {
            GameCatalog catalog=Data<GameCatalog>("GameCatalog");
            string[] terrainPrefixes={"05_","01_","02_","03_","04_","06_","07_","08_","09_","11_","10_"};
            catalog.terrains=new HexData[11];
            for(int i=0;i<11;i++)
            {
                TerrainKind kind=(TerrainKind)i; HexData data=Data<HexData>("Terrain_"+kind); data.kind=kind;
                data.terrain=Art(Find("Terrain_Hex_11_SVG_PNG",terrainPrefixes[i]));
                data.forest=i>=1&&i<=7?Art(Find("Forest_Hex_7_SVG_PNG",i.ToString("00")+"_")):null;
                data.moveCost=kind==TerrainKind.Mountains?3:kind==TerrainKind.Hills||kind==TerrainKind.Swamp?2:1;
                data.foodYield=kind==TerrainKind.Arctic||kind==TerrainKind.Mountains?0:kind==TerrainKind.Desert||kind==TerrainKind.Hills?1:2;
                catalog.terrains[i]=data;
            }
            string[] names={"Free Colonist","Soldier","Hardy Pioneer","Plains Indian","Tropical Indian","Scout","Farmer","Fisherman","Lumberjack","Carpenter","Ore Miner","Blacksmith","Fur Trapper","Fur Trader","Cotton Planter","Cotton Weaver","Missionary","Statesman","Gunsmith","Criminal"};
            catalog.landUnits=new UnitData[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                UnitData data=Data<UnitData>("Land_"+(i+1).ToString("00")+"_"+names[i].Replace(' ','_'));data.displayName=names[i];
                data.art=Art(Find("unit_icons_256",(i+1).ToString("00")+"_"));data.naval=false;data.pioneer=i==2;data.colonist=i==0;
                data.movement=i==5?5:3;data.strength=i==1?4:1;data.capacity=0;catalog.landUnits[i]=data;
            }
            string[] shipNames={"Caravel","Merchantman","Galleon","Privateer","Frigate","Man Of War"};
            int[] capacity={2,4,6,2,4,6};catalog.ships=new UnitData[6];
            for(int i=0;i<6;i++)
            {
                UnitData data=Data<UnitData>("Ship_"+shipNames[i].Replace(' ','_'));data.displayName=shipNames[i];
                data.art=Art(Find("naval_unit_icons_256",(i+1).ToString("00")+"_"));data.naval=true;data.movement=5;data.strength=i+2;data.capacity=capacity[i];
                catalog.ships[i]=data;
            }
            string[] folders=Directory.GetDirectories(ArtRoot+"Native_Nations_8").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            catalog.nativeNations=new NativeNationData[folders.Length];
            for(int i=0;i<folders.Length;i++)
            {
                string folder=folders[i].Replace('\\','/')+"/";
                NationJson json=JsonUtility.FromJson<NationJson>(File.ReadAllText(folder+"data.json"));
                NativeNationData data=Data<NativeNationData>("Nation_"+json.name.Replace(' ','_'));
                data.displayName=json.name;data.leader=json.leader;data.date=json.date;data.biography=json.bio;data.region=json.region;
                data.terrain=json.terrain;data.goods=json.goods;data.jobs=json.jobs;data.rival=json.enemy;data.scenarioAggression=json.aggressive;data.contextNote=json.note;data.sources=json.sources;
                data.village=Art(folder+"village_256x256.png");data.portrait=Sprite(folder+"leader_512x1024.png");
                data.emblem=Sprite(folder+"flag_game_emblem.png");data.profile=Sprite(folder+"profile.jpg");catalog.nativeNations[i]=data;
            }
            return catalog;
        }
        private static SpriteRenderer Renderer(Transform parent,Material material)
        {
            var renderer=new GameObject("Artwork",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();renderer.transform.SetParent(parent,false);renderer.sharedMaterial=material;return renderer;
        }
        private static Hex HexPrefab(Material material)
        {
            var obj=new GameObject("Hex",typeof(Hex),typeof(PolygonCollider2D));
            obj.GetComponent<Hex>().ConfigureRenderer(Renderer(obj.transform,material));
            obj.GetComponent<Hex>().ConfigureVisibility(true);
            var points=new Vector2[6];for(int i=0;i<6;i++){float a=(90+i*60)*Mathf.Deg2Rad;points[i]=new Vector2(Mathf.Cos(a)*1.5f,Mathf.Sin(a)*1.5f);}
            obj.GetComponent<PolygonCollider2D>().points=points;
            GameObject prefab=PrefabUtility.SaveAsPrefabAsset(obj,PrefabRoot+"Hex.prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab.GetComponent<Hex>();
        }
        private static Unit UnitPrefab(Material material)
        {
            var obj=new GameObject("Unit",typeof(Unit));obj.GetComponent<Unit>().ConfigureRenderer(Renderer(obj.transform,material));
            GameObject prefab=PrefabUtility.SaveAsPrefabAsset(obj,PrefabRoot+"Unit.prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab.GetComponent<Unit>();
        }
        public static void ValidateCatalog(GameCatalog catalog)
        {
            if(catalog.terrains.Length!=11||catalog.landUnits.Length!=20||catalog.ships.Length!=6||catalog.nativeNations.Length!=8)
                throw new InvalidOperationException("Unexpected catalog counts.");
            foreach(HexData terrain in catalog.terrains) if(terrain.terrain?.sprite==null) throw new InvalidOperationException("Terrain missing artwork.");
            foreach(UnitData unit in catalog.landUnits.Concat(catalog.ships)) if(unit.art?.sprite==null) throw new InvalidOperationException("Unit missing artwork.");
            foreach(NativeNationData nation in catalog.nativeNations)
                if(nation.portrait==null||nation.emblem==null||nation.village?.sprite==null||nation.profile==null) throw new InvalidOperationException("Nation missing artwork.");
        }
        public static void EnableWalkthroughVisibility()
        {
            string path=PrefabRoot+"Hex.prefab";
            GameObject root=PrefabUtility.LoadPrefabContents(path);
            try { root.GetComponent<Hex>().ConfigureVisibility(true);PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("COLONIZATION_ALL_HEXES_VISIBLE");
        }
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Colonization");
            var result=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/Colonization/Colonization.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: "+result.summary.result);
            Debug.Log("COLONIZATION_WINDOWS_BUILD_OK");
        }
    }
}
