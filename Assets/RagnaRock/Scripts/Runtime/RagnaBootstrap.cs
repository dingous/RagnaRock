using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RagnaRock
{
    [DisallowMultipleComponent]
    public sealed class RagnaBootstrap : MonoBehaviour
    {
        private string failure;
        private void Start()
        {
            if(GameSession.Instance!=null){Destroy(gameObject);return;}
            try
            {
                Application.targetFrameRate=60;
                var session=gameObject.AddComponent<GameSession>();session.Initialize();
            }
            catch(Exception ex)
            {
                failure="RagnaRock não conseguiu iniciar.\n\n"+ex.Message+
                    "\n\nAbra o Console do Unity para o diagnóstico.\nMenu: RagnaRock > Validar projeto.";
                Debug.LogException(ex);
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEntryPoint()
        {
            if(SceneManager.GetActiveScene().name!="RagnaRock")return;
            if(FindFirstObjectByType<RagnaBootstrap>()==null)new GameObject("RagnaRock").AddComponent<RagnaBootstrap>();
        }
        private void OnGUI()
        {
            if(string.IsNullOrEmpty(failure))return;
            GUI.color=Color.white;
            var style=new GUIStyle(GUI.skin.box){fontSize=20,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            GUI.Box(new Rect(30,30,Mathf.Max(280,Screen.width-60),Mathf.Max(200,Screen.height-60)),failure,style);
        }
    }
}
