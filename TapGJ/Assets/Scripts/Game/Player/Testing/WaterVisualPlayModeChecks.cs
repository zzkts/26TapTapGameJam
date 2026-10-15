#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TapGJ.WaterProjectiles;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

// MCP checks against actual player input and the production projectile, with temporary fixtures only.
public sealed class WaterVisualPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running { get; private set; }
    public static bool Succeeded=>!Running&&Results.Count>0&&Results.All(x=>x.StartsWith("PASS:"));
    public static string Stage { get; private set; }
    private readonly List<GameObject> fixtures=new();
    private readonly List<Behaviour> suspended=new();
    private Mouse mouse,previousMouse;
    private Keyboard keyboard,previousKeyboard;
    private InputSettings.BackgroundBehavior background;
    private InputSettings.EditorInputBehaviorInPlayMode editorInput;
    private PlayerController player;
    private bool validation;
    private string captureDir="Docs/WaterVisualValidation/";

    public void Begin()
    {
        Results.Clear();Running=true;Stage="Real held fire";
        player=Object.FindFirstObjectByType<PlayerController>();
        previousMouse=Mouse.current;previousKeyboard=Keyboard.current;
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
        validation=StorySession.Current.ValidationMode;StorySession.Current.SetValidationMode(true);
        foreach(var a in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
            if(a.enabled&&(a is EnemyAIController||a is EnemySupportAura)){suspended.Add(a);a.enabled=false;}
        System.IO.Directory.CreateDirectory(captureDir);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait=null;
            try
            {
                var current=stack.Peek();if(!current.MoveNext()){stack.Pop();continue;}
                wait=current.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}
            }
            catch(Exception e){Results.Add("FAIL: "+e);break;}
            yield return wait;
        }
        Cleanup();Running=false;Stage="complete";
        Debug.Log("Water visual checks: "+string.Join("\n",Results));
    }

    private IEnumerator Cases()
    {
        Time.timeScale=1;player.SetInvincible(true);player.SetMenuOpen(false);
        Check(player.Weapon.BaseData.projectilePrefab.GetComponent<Projectile>().ElementType==E_ElementType.Water,
            "Default player weapon references a water projectile");
        Vector2 aim=(Vector2)player.transform.position+new Vector2(6,2);
        int shotsBefore=player.Presentation.ShotCount;
        Aim(aim,true);yield return new WaitForSeconds(.52f);
        var water=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(x=>x.ElementType==E_ElementType.Water).ToArray();
        Check(player.Presentation.ShotCount>=shotsBefore+2,"Real held mouse input produces repeated water shots");
        var shot=water.Last();var surface=shot.GetComponent<WaterProjectileVisual>();
        Check(surface!=null&&surface.Surface.sharedMaterial.shader.name=="TapGJ/Liquid Drop 2D",
            "Launched water keeps its liquid shader instead of the generic element shader");
        Check(!shot.GetComponent<SpriteRenderer>().enabled,"The old round sprite is hidden");
        Check(Mathf.Abs(shot.GetComponent<CircleCollider2D>().radius-.5f)<.001f&&
            Mathf.Abs(shot.transform.localScale.x-shot.Snapshot.size)<.001f,
            "Liquid deformation leaves the actual hitbox and gene size unchanged");
        Check(surface.Surface.transform.localScale.x>surface.Surface.transform.localScale.y,
            "Moving water stretches along its trajectory");
        Check(Mathf.Sin(surface.Surface.transform.eulerAngles.z*Mathf.Deg2Rad)>.1f,
            "Water body aligns to diagonal player aim");
        var trail=shot.GetComponentInChildren<TrailRenderer>();
        Check(trail!=null&&trail.time<.15f&&trail.sharedMaterial.GetFloat("_Kind")==1,
            "Water uses a short translucent ligament rather than a glowing ribbon");
        Check(water.Sum(p=>p.GetComponentsInChildren<ParticleSystem>().Sum(s=>s.particleCount))>0,
            "Continuous flight sheds separate droplets");
        ScreenCapture.CaptureScreenshot(captureDir+"Held-Water-Fire.png");
        Aim(aim,false);yield return new WaitForSeconds(.12f);
        foreach(var p in water)if(p!=null)Object.Destroy(p.gameObject);
        Stage="Impact and trajectory fixtures";
        var wall=new GameObject("Water QA Wall");fixtures.Add(wall);wall.layer=3;
        wall.transform.position=new Vector3(21,1.7f,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.18f,1.6f);
        var data=player.Weapon.BaseData;var impact=CreateShot(new Vector2(19,1.7f),Vector2.right,new BulletSnapshot(data){speed=8,size=.65f});
        yield return new WaitForSeconds(.23f);
        Check(impact==null,"Real wall collision consumes the water shot");
        var spray=Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).FirstOrDefault(p=>p.name=="LiquidImpactSpray");
        Check(spray!=null&&spray.particleCount>=10,"Impact separates into a fan of water drops");
        var drops=new ParticleSystem.Particle[32];int count=spray.GetParticles(drops);
        Check(drops.Take(count).Average(d=>d.position.x)>18&&drops.Take(count).Average(d=>d.position.x)<22,
            "Impact particles originate at the world contact, not the world origin");
        Check(drops.Take(count).All(d=>d.velocity.x<.01f),"Wall splash travels out from the contact normal");
        Check(spray.main.gravityModifier.constant>.5f,"Splash droplets fall under gravity");
        Check(Object.FindFirstObjectByType<WaterImpactSheet>()!=null,"Impact creates a brief surface sheet");
        ScreenCapture.CaptureScreenshot(captureDir+"Wall-Splash.png");
        // Put the bounce fixture above ambient water fields and material triggers.
        wall.transform.position=new Vector3(21,4.4f,0);
        var bounce=CreateShot(new Vector2(19,4.4f),Vector2.right,new BulletSnapshot(data){speed=8,size=.35f,reboundCount=1,reboundCoefficient=1});
        yield return new WaitForSeconds(.35f);
        Check(bounce!=null&&Mathf.Cos(bounce.GetComponent<WaterProjectileVisual>().Surface.transform.eulerAngles.z*Mathf.Deg2Rad)<-.9f,
            "Rebound reverses the liquid body with the actual shot"+(bounce==null?" (shot expired)":" (angle "+bounce.GetComponent<WaterProjectileVisual>().Surface.transform.eulerAngles.z+", x "+bounce.transform.position.x+")"));
        if(bounce!=null)Object.Destroy(bounce.gameObject);
        var orbit=CreateShot((Vector2)player.transform.position+Vector2.up*2,Vector2.up,
            new BulletSnapshot(data){speed=2,size=.35f,moveType=E_ProjectileMoveType.Orbit,orbitRadius=1,orbitAngularSpeed=120});
        yield return new WaitForSeconds(.15f);
        float first=orbit.GetComponent<WaterProjectileVisual>().Surface.transform.eulerAngles.z;
        yield return new WaitForSeconds(.2f);
        Check(Mathf.Abs(Mathf.DeltaAngle(first,orbit.GetComponent<WaterProjectileVisual>().Surface.transform.eulerAngles.z))>5,
            "Orbiting water continually follows its tangent");
        Object.Destroy(orbit.gameObject);
        yield return new WaitForSeconds(.8f);
        Check(Object.FindFirstObjectByType<WaterImpactSheet>()==null&&
            !Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(x=>x.name=="LiquidImpactSpray"),
            "Impact effects clean up after their short lifetime");
        Stage="Rendered liquid surface";yield return RenderCases();
    }

    private IEnumerator RenderCases()
    {
        // A normal production shot rendered against a known backing color verifies translucency and highlights.
        var cameraObj=new GameObject("Liquid QA Camera");fixtures.Add(cameraObj);
        var camera=cameraObj.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.65f;
        camera.transform.position=new Vector3(100,100,-10);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.28f,.15f,.34f);camera.cullingMask=1<<10;
        var shot=CreateShot(new Vector2(100,100),Vector2.right,new BulletSnapshot(player.Weapon.BaseData){speed=0,size=1,lifeTime=5});
        yield return null;
        var rt=new RenderTexture(512,512,24);var tex=new Texture2D(512,512,TextureFormat.RGB24,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,512,512),0,0);tex.Apply();
            Color center=tex.GetPixel(256,256),back=tex.GetPixel(20,20);
            Check(center.b>center.r&&center.g>back.g+.12f,"Actual shader renders a blue water body");
            Check(tex.GetPixels().Any(c=>c.r>.6f&&c.g>.7f&&c.b>.7f),"Curved surface includes a bright reflected highlight");
            Check(Mathf.Abs(center.r-back.r)<.23f&&center.r>.07f,"The backing color remains visible through the water body");
            System.IO.File.WriteAllBytes(captureDir+"Water-Surface-Closeup.png",tex.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(tex);}
    }

    private Projectile CreateShot(Vector2 at,Vector2 heading,BulletSnapshot snap)
    {var obj=Object.Instantiate(snap.projectilePrefab);fixtures.Add(obj);var p=obj.GetComponent<Projectile>();p.LaunchSnapshot(at,heading,0,player.transform,snap,new ShotSpawnBudget(24));return p;}
    private void Aim(Vector2 at,bool fire){mouse.MakeCurrent();var state=new MouseState{position=Camera.main.WorldToScreenPoint(at)};if(fire)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
    private void Check(bool ok,string label){Results.Add((ok?"PASS: ":"FAIL: ")+label);}
    private void Cleanup()
    {
        InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);previousMouse?.MakeCurrent();previousKeyboard?.MakeCurrent();
        InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
        foreach(var a in suspended)if(a!=null)a.enabled=true;
        foreach(var f in fixtures)if(f!=null)Object.Destroy(f);
        player.SetInvincible(false);StorySession.Current.SetValidationMode(validation);
    }
}
#endif
