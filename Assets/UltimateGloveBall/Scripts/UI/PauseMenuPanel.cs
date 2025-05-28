using UnityEngine;
using System.Threading.Tasks;

namespace PongHub.UI
{
    public class PauseMenuPanel : MonoBehaviour
    {
        public async Task InitializeAsync()
        {
            await Task.Yield();
        }
    }
} 