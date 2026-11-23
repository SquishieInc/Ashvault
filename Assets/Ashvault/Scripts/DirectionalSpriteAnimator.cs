using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    // Atlas rows are laid out TOP first, columns: front, front-right, right,
    // rear-right, back, rear-left, left, front-left. Sprite.Create uses bottom origin.
    // v3 8x6 atlases remain usable; v4 reaver adds 11 distinct timed pose rows.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        private const int Directions=8, CellWidth=96, CellHeight=144;
        private static readonly Dictionary<string,Sprite[]> Atlases=new Dictionary<string,Sprite[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAtlases() => Atlases.Clear();

        private SpriteRenderer artwork;
        private Transform actor;
        private Sprite[] frames;
        private int rowCount;
        private float actionBegan,seed;
        private enum Action { None,Attack,Hurt,Death }
        private Action action;
        private bool dead;

        public void Setup(string baseSprite,Transform facingActor)
        {
            artwork=GetComponent<SpriteRenderer>();
            actor=facingActor;
            seed=Mathf.Abs((actor.position.x*7.2f+actor.position.z*3.9f)%19f);
            frames=GetAtlas(baseSprite);
            rowCount=frames == null ? 0 : frames.Length/Directions;
            artwork.sharedMaterial=VisualFactory.LitSpriteMaterial(frames!=null ?
                "Directional/"+baseSprite+"_directions" : baseSprite);
            artwork.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            artwork.receiveShadows=true;
            SetFrame(0,0);
        }
        private static Sprite[] GetAtlas(string kind)
        {
            if (Atlases.TryGetValue(kind,out var found))
            {
                if (found != null && found.Length > 0 && found[0] != null) return found;
                Atlases.Remove(kind);
            }
            var tex=Resources.Load<Texture2D>("Textures/Directional/"+kind+"_directions");
            if(tex==null||tex.width!=CellWidth*Directions ||
                tex.height%CellHeight!=0 || (tex.height/CellHeight !=6 && tex.height/CellHeight!=11))
            {
                Debug.LogWarning("Ashvault: directional atlas missing/wrong size for "+kind);
                return null;
            }
            tex.filterMode=FilterMode.Point;
            int rows=tex.height/CellHeight;
            found=new Sprite[Directions*rows];
            for(int row=0;row<rows;row++)for(int dir=0;dir<Directions;dir++)
                found[row*Directions+dir]=Sprite.Create(tex,
                    new Rect(dir*CellWidth,(rows-row-1)*CellHeight,CellWidth,CellHeight),
                    new Vector2(.5f,.5f),64f,0,SpriteMeshType.FullRect);
            Atlases[kind]=found;
            return found;
        }
        public void PlayAttack() {if (!dead) {action=Action.Attack;actionBegan=Time.time;} }
        public void PlayHurt() {if (!dead) {action=Action.Hurt;actionBegan=Time.time;} }
        public void PlayDeath() {dead=true;action=Action.Death;actionBegan=Time.time;}

        private void LateUpdate()
        {
            if(frames==null||actor==null||Camera.main==null)return;
            Vector3 towardCamera=Camera.main.transform.position-actor.position;
            towardCamera.y=0;
            if(towardCamera.sqrMagnitude < .001f)return;
            Vector3 local=actor.InverseTransformDirection(towardCamera.normalized);
            int angle=(Mathf.RoundToInt(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/45f)+8)%8;
            float elapsed=Time.time-actionBegan;
            int row=0;
            if(rowCount==11)
            {
                switch(action)
                {
                    case Action.Attack:
                        if(elapsed < .11f)row=3;
                        else if(elapsed < .24f)row=4;
                        else if(elapsed < .38f)row=5;
                        else {action=Action.None;row=0;}
                        break;
                    case Action.Hurt:
                        if(elapsed < .24f)row=6;
                        else {action=Action.None;row=0;}
                        break;
                    case Action.Death:
                        row=elapsed < .14f ? 7 : elapsed < .36f ? 8 : elapsed < .69f ? 9 : 10;
                        break;
                    default: row=Moving() ? 1+(Mathf.FloorToInt((Time.time+seed)*6f)&1):0;break;
                }
            }
            else // legacy 8x6 files; retain playback while art is replaced per enemy.
            {
                if(dead)row=5;
                else if(action==Action.Attack && elapsed < .32f)row=3;
                else if(action==Action.Hurt && elapsed < .23f)row=4;
                else {action=Action.None;row=Moving()?1+(Mathf.FloorToInt((Time.time+seed)*6f)&1):0;}
            }
            SetFrame(row,angle);
        }
        private bool Moving()
        {
            var enemy=actor.GetComponent<EnemyAI>();
            return enemy!=null&&enemy.IsVisuallyMoving;
        }
        private void SetFrame(int row,int dir)
        {
            if(artwork==null||frames==null)return;
            Sprite sprite=frames[Mathf.Clamp(row,0,rowCount-1)*Directions+dir];
            if(artwork.sprite!=sprite)artwork.sprite=sprite;
        }
    }
}
