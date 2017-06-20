using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UniRx;

using Lidgren.Network;


namespace FPS
{
    public class TestConnection : MonoBehaviour, ICancelable
    {
        public bool IsDisposed { get; private set; }

        private void Awake()
        {
            CancellationToken _cancellationToken = new CancellationToken(this);

            ConfigureServer(_cancellationToken);
            ConfigureClient(_cancellationToken);
        }


        private void OnDestroy()
        {
            Dispose();
        }


        private void ConfigureServer(CancellationToken cancellationToken)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("MyExampleName");
            config.Port = 14242;

            NetServer server = new NetServer(config);
            server.Start();

            MessageLoop(server, cancellationToken).Subscribe(Debug.Log).AddTo(this);

            Observable.Timer(TimeSpan.FromSeconds(3f)).Subscribe(_ =>
            {
                NetOutgoingMessage outgoingMessage = server.CreateMessage();
                outgoingMessage.Write("Hello!");
                outgoingMessage.Write("World!");
                server.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            });
        }

        private void ConfigureClient(CancellationToken cancellationToken)
        {
            var config = new NetPeerConfiguration("MyExampleName");
            var client = new NetClient(config);
            client.Start();
            client.Connect(host: "127.0.0.1", port: 14242);

            MessageLoop(client, cancellationToken).Subscribe(Debug.Log).AddTo(this);
        }

        private IObservable<string> MessageLoop(NetPeer peer, CancellationToken cancellationToken)
        {
            return Observable.Create<string>(observer =>
            {
                NetIncomingMessage message;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if((message = peer.ReadMessage()) == null)
                    {
                        continue;
                    }

                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            var data = message.ReadString();
                            observer.OnNext(data);
                            break;
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            observer.OnNext(message.ReadString());
                            break;
                        default:
                            observer.OnNext("Unhandled type: " + message.MessageType);
                            break;
                    }

                    peer.Recycle(message);
                }

                return Disposable.Create(() =>
                {

                });
            }).
            SubscribeOn(Scheduler.ThreadPool).
            ObserveOnMainThread();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
