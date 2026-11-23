using UnityEngine;

namespace Ashvault
{
    // Raycast-interactable lever; animation is mechanical, no PlayerPrefs or persistent switch state.
    public sealed class AshSwitch : MonoBehaviour, IUsable
    {
        private LockedDoor target;
        private Transform lever;
        private Renderer lamp;
        private bool pressed;
        private float started;
        private Vector3 initial;
        private Material idle, activated;

        public static void Create(Transform root, DungeonMap map, DoorSwitchData data, LockedDoor door)
        {
            if (!map.IsOpen(data.x,data.z)) return;
            var body=GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name="SWITCH / "+(string.IsNullOrEmpty(data.name)?"rune lever":data.name);
            body.transform.SetParent(root,false);
            body.transform.position=map.World(data.x,data.z)+Vector3.up*1.38f;
            body.transform.localScale=new Vector3(.68f,.82f,.25f);
            body.GetComponent<MeshRenderer>().sharedMaterial=VisualFactory.StoneMaterial("door_runes",new Color(.41f,.35f,.37f));
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name="PULL / LEVER";
            plate.transform.SetParent(body.transform,false);
            plate.transform.localScale=new Vector3(.34f,.66f,.30f);
            plate.transform.localPosition=new Vector3(0,0,-.64f);
            Destroy(plate.GetComponent<Collider>());
            plate.GetComponent<Renderer>().sharedMaterial=VisualFactory.StoneMaterial("door_runes",new Color(.93f,.50f,.25f));
            var signal=GameObject.CreatePrimitive(PrimitiveType.Cube);
            signal.name="STATUS / SIGIL LAMP";
            signal.transform.SetParent(body.transform,false);
            signal.transform.localPosition=new Vector3(0,.30f,-.66f);
            signal.transform.localScale=new Vector3(.20f,.12f,.12f);
            Destroy(signal.GetComponent<Collider>());
            var sw=body.AddComponent<AshSwitch>();
            sw.target=door;
            sw.lever=plate.transform;
            sw.initial=plate.transform.localPosition;
            sw.lamp=signal.GetComponent<MeshRenderer>();
            sw.idle=VisualFactory.StoneMaterial("door_runes",new Color(.85f,.21f,.18f));
            sw.activated=VisualFactory.StoneMaterial("door_runes",new Color(.24f,.95f,.38f));
            sw.lamp.sharedMaterial=sw.idle;
        }
        public void Use(AshPlayer actor)
        {
            if (actor == null || target == null || pressed) return;
            target.Use(actor); // The door still checks class and required keys.
            if (!target.IsOpening) return;
            pressed=true;
            started=Time.time;
            if (lamp!=null)lamp.sharedMaterial=activated;
            AshAudio.OneShot("switch_press",transform.position,.75f);
        }
        private void Update()
        {
            if (!pressed||lever==null)return;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01((Time.time-started)/.35f));
            lever.localPosition=initial+Vector3.down*t*.12f;
            lever.localRotation=Quaternion.Euler(65f*t,0f,0f);
        }
    }

    // Two physical-looking wings share a still-full-sized collider on the parent
    // so no character can corner-cut before the seal is fully open.
    public sealed class SplitDoorFacade : MonoBehaviour
    {
        private LockedDoor motor;
        private Transform left,right;
        private Vector3 leftOrigin,rightOrigin;
        public static void Create(GameObject slab, LockedDoor controller)
        {
            var original=slab.GetComponent<MeshRenderer>();
            var facade=slab.AddComponent<SplitDoorFacade>();
            facade.motor=controller;
            facade.left=MakePanel(slab.transform,"LEFT / RUNE DOOR",-.25f,original.sharedMaterial);
            facade.right=MakePanel(slab.transform,"RIGHT / RUNE DOOR",.25f,original.sharedMaterial);
            facade.leftOrigin=facade.left.localPosition;
            facade.rightOrigin=facade.right.localPosition;
            original.enabled=false;
        }
        private static Transform MakePanel(Transform parent,string name,float x,Material mat)
        {
            var child=GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name=name;
            child.transform.SetParent(parent,false);
            child.transform.localPosition=new Vector3(x,0,0);
            child.transform.localScale=new Vector3(.5f,1f,1f);
            child.GetComponent<MeshRenderer>().sharedMaterial=mat;
            Destroy(child.GetComponent<Collider>());
            return child.transform;
        }
        private void Update()
        {
            if(motor==null)return;
            float t=motor.OpenAmount;
            left.localPosition=leftOrigin+Vector3.left*.52f*t;
            right.localPosition=rightOrigin+Vector3.right*.52f*t;
        }
    }
}
