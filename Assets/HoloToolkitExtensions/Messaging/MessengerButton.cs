using System;
using UnityEngine;

namespace HoloToolkitExtensions.Messaging
{
    public class MessengerButton<T> : MonoBehaviour where T:class, new()
    {
        public void Click()
        {
            Messenger.Instance.Broadcast(new T());
        }
    }
}
