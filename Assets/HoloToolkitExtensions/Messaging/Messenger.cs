
using System;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;

namespace HoloToolkitExtensions.Messaging
{
    //Stolen and adapted from http://wiki.unity3d.com/index.php?title=CSharpMessenger_Extended
    public class Messenger : Singleton<Messenger>
    {
        #region Internal variables


        public Dictionary<Type, Delegate> EventTable = new Dictionary<Type, Delegate>();

        //Message handlers that should never be removed, regardless of calling Cleanup
        public List<Type> PermanentMessages = new List<Type>();

        #endregion

        #region Helper methods

        //Marks a certain message as permanent.
        public void MarkAsPermanent(Type eventType)
        {
#if LOG_ALL_MESSAGES
		Debug.Log("Messenger MarkAsPermanent \t\"" + eventType + "\"");
#endif

            PermanentMessages.Add(eventType);
        }


        public void Cleanup()
        {
#if LOG_ALL_MESSAGES
		Debug.Log("MESSENGER Cleanup. Make sure that none of necessary listeners are removed.");
#endif

            var messagesToRemove = new List<Type>();

            foreach (var pair in EventTable)
            {
                bool wasFound = false;

                foreach (Type message in PermanentMessages)
                {
                    if (pair.Key == message)
                    {
                        wasFound = true;
                        break;
                    }
                }

                if (!wasFound)
                    messagesToRemove.Add(pair.Key);
            }

            foreach (var message in messagesToRemove)
            {
                EventTable.Remove(message);
            }
        }

        public void PrintEventTable()
        {
            Debug.Log("\t\t\t=== MESSENGER PrintEventTable ===");

            foreach (var pair in EventTable)
            {
                Debug.Log("\t\t\t" + pair.Key + "\t\t" + pair.Value);
            }

            Debug.Log("\n");
        }

        #endregion

        #region Message logging and exception throwing

        protected void OnListenerAdding(Type eventType, Delegate listenerBeingAdded)
        {
#if LOG_ALL_MESSAGES || LOG_ADD_LISTENER
		Debug.Log("MESSENGER OnListenerAdding \t\"" + eventType + "\"\t{" + listenerBeingAdded.Target + " -> " + listenerBeingAdded.Method + "}");
#endif

            if (!EventTable.ContainsKey(eventType))
            {
                EventTable.Add(eventType, null);
            }

            Delegate d = EventTable[eventType];
            if (d != null && d.GetType() != listenerBeingAdded.GetType())
            {
                throw new ListenerException(
                    string.Format(
                        "Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}",
                        eventType, d.GetType().Name, listenerBeingAdded.GetType().Name));
            }
        }

        protected void OnListenerRemoving(Type eventType, Delegate listenerBeingRemoved)
        {
#if LOG_ALL_MESSAGES
		Debug.Log("MESSENGER OnListenerRemoving \t\"" + eventType + "\"\t{" + listenerBeingRemoved.Target + " -> " + listenerBeingRemoved.Method + "}");
#endif

            if (EventTable.ContainsKey(eventType))
            {
                Delegate d = EventTable[eventType];

                if (d == null)
                {
                    throw new ListenerException(
                        string.Format(
                            "Attempting to remove listener with for event type \"{0}\" but current listener is null.",
                            eventType));
                }
                else if (d.GetType() != listenerBeingRemoved.GetType())
                {
                    throw new ListenerException(
                        string.Format(
                            "Attempting to remove listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being removed has type {2}",
                            eventType, d.GetType().Name, listenerBeingRemoved.GetType().Name));
                }
            }
            else
            {
                throw new ListenerException(
                    string.Format(
                        "Attempting to remove listener for type \"{0}\" but Messenger doesn't know about this event type.",
                        eventType));
            }
        }

        protected void OnListenerRemoved(Type eventType)
        {
            if (EventTable[eventType] == null)
            {
                EventTable.Remove(eventType);
            }
        }

        protected void OnBroadcasting(Type eventType)
        {

        }

        protected BroadcastException CreateBroadcastSignatureException(Type eventType)
        {
            return
                new BroadcastException(
                    string.Format(
                        "Broadcasting message \"{0}\" but listeners have a different signature than the broadcaster.",
                        eventType));
        }

        public class BroadcastException : Exception
        {
            public BroadcastException(string msg)
                : base(msg)
            {
            }
        }

        public class ListenerException : Exception
        {
            public ListenerException(string msg)
                : base(msg)
            {
            }
        }

        #endregion

        #region AddListener

        //No parameters
        public void AddListener<T>( MessengerCallback<T> handler)
        {
            var messageType = typeof(T);
            OnListenerAdding(messageType, handler);
            EventTable[messageType] = (MessengerCallback<T>)EventTable[messageType] + handler;

        }


        #endregion

        #region RemoveListener

        //No parameters
        public void RemoveListener<T>(MessengerCallback<T> handler)
        {
            var messageType = typeof(T);
            OnListenerRemoving(messageType, handler);
            EventTable[messageType] = (MessengerCallback<T>)EventTable[messageType] - handler;
            OnListenerRemoved(messageType);
        }


        #endregion

        //No parameters
        public void Broadcast<T>(T eventType)
        {
#if LOG_ALL_MESSAGES || LOG_BROADCAST_MESSAGE
		Debug.Log("MESSENGER\t" + System.DateTime.Now.ToString("hh:mm:ss.fff") + "\t\t\tInvoking \t\"" + eventType + "\"");
#endif
            var messageType = eventType.GetType();
            OnBroadcasting(messageType);

            Delegate d;
            if (EventTable.TryGetValue(messageType, out d))
            {
                d.DynamicInvoke(eventType);
            }
        }
    }
}



