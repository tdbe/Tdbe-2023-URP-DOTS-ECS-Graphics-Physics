
using UnityEngine;

// NOTE:
// The reason I have this is I need to transfer (camera) data from the Main Scene to the Entities Subscene.
// I spent a couple hours trying to find what's the ECS way of doing this, but all I could find
// was same-scene managed component references.
namespace HackyGlobals
{
    // So here I am just taking data from the camera if/when the viewport is resized,
    // and setting it as static data so I can hackily access it in the entities system.
    public class WorldBounds : MonoBehaviour
    {
        [SerializeField]
        Camera m_camera;
        [SerializeField]
        Transform background;
        
        static (Vector3, Vector3)[] m_boundsPosAndScaleArrayBottomClockwise;
        public static (Vector3, Vector3)[] _boundsPosAndScaleArrayBottomClockwise{get{return m_boundsPosAndScaleArrayBottomClockwise;}}
        static bool m_haveChanged;
        public static bool _haveChanged { get{return m_haveChanged;}}

        Vector2 m_screenSize;
        void Awake(){
        }

        // Start is called before the first frame update
        void Start()
        {
            if(m_camera == null){
                m_camera = Camera.main;
            }
            //m_screenSize = new Vector2(Screen.width, Screen.height);

            m_boundsPosAndScaleArrayBottomClockwise = new (Vector3, Vector3)[4];
        }

        // Update is called once per frame
        void Update()
        {
            if(m_screenSize.x != Screen.width || m_screenSize.y != Screen.height)
            {
                m_haveChanged = true;
                Debug.Log("[WorldBounds] Screen Size changed: from {"+m_screenSize.x+", "+m_screenSize.y+"}; to {"+Screen.width+", "+Screen.height+"}");
                m_screenSize = new Vector2(Screen.width, Screen.height);
                
                Vector3 viewportWorldPos = m_camera.WorldToViewportPoint(Vector3.zero);

                Vector3[] worldCorners = {
                    //bottomLeft
                    m_camera.ViewportToWorldPoint(new Vector3(0f, 0f, viewportWorldPos.z)),
                    //topLeft
                    m_camera.ViewportToWorldPoint(new Vector3(0f, 1f, viewportWorldPos.z)),
                    //topRight
                    m_camera.ViewportToWorldPoint(new Vector3(1f, 1f, viewportWorldPos.z)),
                    //bottomRight
                    m_camera.ViewportToWorldPoint(new Vector3(1f, 0f, viewportWorldPos.z))
                };

                (int, int)[] cc = {(0, 3), (0, 1), (1, 2), (2, 3)};
            
                for(int i = 0; i< m_boundsPosAndScaleArrayBottomClockwise.Length; i++){
                    m_boundsPosAndScaleArrayBottomClockwise[i].Item1 = Vector3.Lerp(worldCorners[cc[i].Item1], worldCorners[cc[i].Item2], .5f);
                    Vector3 scale = Vector3.one;
                    float sv = Vector3.Distance(worldCorners[cc[i].Item1], worldCorners[cc[i].Item2]);
                    //if(i%2==0)
                        scale.x = sv;
                    //else
                    //    scale.y = sv;
                    // ^ commented out because I was forced to rotate instead of scale in different directions, because of shared collider baking T_T
                    m_boundsPosAndScaleArrayBottomClockwise[i].Item2 = scale;

                    if(i==0){
                        background.localScale = new Vector3(sv, sv, 1);
                    }
                }

            }

        }

        void LateUpdate() {
            m_haveChanged = false;
        }
    }
}
