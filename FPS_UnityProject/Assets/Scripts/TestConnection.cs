using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using UnityEngine;

using UniRx;

using Lidgren.Network;
using FPS.Assets.Scripts;

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
                outgoingMessage.Write((new PositionSnapshot(10f)).Serialize());
                //outgoingMessage.Write("World!");
                server.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            });
        }

        private void ConfigureClient(CancellationToken cancellationToken)
        {
            var config = new NetPeerConfiguration("MyExampleName");
            var client = new NetClient(config);
            client.Start();
            client.Connect(host: "127.0.0.1", port: 14242);

            MessageLoop(client, cancellationToken, true).Subscribe(Debug.Log).AddTo(this);
        }

        private IObservable<string> MessageLoop(NetPeer peer, CancellationToken cancellationToken, bool isClient = false)
        {
            return Observable.Create<string>(observer =>
            {
                NetIncomingMessage message;

                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(10);

                    while ((message = peer.ReadMessage()) != null)
                    {
                        switch (message.MessageType)
                        {
                            case NetIncomingMessageType.Data:
                                //var data = message.ReadString();
                                try
                                {
                                    PositionSnapshot ps = new PositionSnapshot(0f);
                                    ps.Deserialize(message.Data);
                                    var data = ps.Position.ToString();

                                    if (isClient)
                                    {
                                        NetOutgoingMessage outgoingMessage = peer.CreateMessage();
                                        outgoingMessage.Write((new PositionSnapshot(22f)).Serialize());
                                        //outgoingMessage.Write("World!");
                                        peer.SendMessage(outgoingMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                    }

                                    observer.OnNext(data);
                                }
                                catch(Exception ex)
                                {
                                    observer.OnNext(ex.Message);
                                }
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                observer.OnNext(message.SenderConnection.Status.ToString());
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
