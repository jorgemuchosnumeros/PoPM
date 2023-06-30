using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PoPM
{
    public class ProtocolWriter : BinaryWriter
    {
        public ProtocolWriter(Stream output) : base(output)
        {
        }

        /// <summary>
        /// Minecraft (lol) VarInt encoding. Adapted from https://wiki.vg/Protocol
        /// </summary>
        public override void Write(int value)
        {
            while (true)
            {
                if ((value & ~0x7F) == 0)
                {
                    Write((byte) value);
                    return;
                }

                Write((byte) ((value & 0x7F) | 0x80));

                value = (int) ((uint) value >> 7);
            }
        }

        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }

        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        public void Write(Vector4 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        public void Write(Quaternion value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        public void Write(Vector2? value)
        {
            if (value.HasValue)
            {
                Write(true);
                Write(value.Value);
            }
            else
                Write(false);
        }

        public void Write(Vector3? value)
        {
            if (value.HasValue)
            {
                Write(true);
                Write(value.Value);
            }
            else
                Write(false);
        }

        public void Write(Vector4? value)
        {
            if (value.HasValue)
            {
                Write(true);
                Write(value.Value);
            }
            else
                Write(false);
        }

        public void Write(Quaternion? value)
        {
            if (value.HasValue)
            {
                Write(true);
                Write(value.Value);
            }
            else
                Write(false);
        }

        public void Write(int[] value)
        {
            Write(value.Length);
            foreach (int n in value)
            {
                Write(n);
            }
        }

        public void Write(float[] value)
        {
            Write(value.Length);
            foreach (float n in value)
            {
                Write(n);
            }
        }

        public void Write(bool[] value)
        {
            Write(value.Length);
            foreach (bool n in value)
            {
                Write(n);
            }
        }

        public void Write(BitArray value)
        {
            var bytes = new byte[(value.Length - 1) / 8 + 1];
            value.CopyTo(bytes, 0);
            Write(bytes.Length);
            Write(bytes);
        }

        public void Write(ActorPacket value)
        {
            Write(value.ID);
            Write(value.Name);
            Write(value.SteamID);
            Write(value.Position);
            Write(value.FacingDirection);
            Write(value.Flags);
            Write(value.JSONSkin);
        }

        public void Write(ActorStateFlags value)
        {
            Write(value.Disconnected);
            Write(value.Dead);
        }

        public void Write(Packet value)
        {
            Write((int) value.ID);
            Write(value.Sender.ToByteArray());
            Write(value.Data.Length);
            Write(value.Data);
        }

        public void Write(GameStatePacket value)
        {
            Write(value.ID);
            Write(value.Name);
            Write(value.EruptionTrigger);
        }
    }

    public class ProtocolReader : BinaryReader
    {
        public ProtocolReader(Stream input) : base(input)
        {
        }

        /// <summary>
        /// Adapted from https://wiki.vg/Protocol
        /// </summary>
        public override int ReadInt32()
        {
            int value = 0;
            int position = 0;
            byte currentByte;

            while (true)
            {
                currentByte = ReadByte();
                value |= (currentByte & 0x7F) << position;

                if ((currentByte & 0x80) == 0) break;

                position += 7;

                if (position >= 32) throw new ArithmeticException("VarInt is too big");
            }

            return value;
        }

        public Vector2 ReadVector2()
        {
            return new Vector2
            {
                x = ReadSingle(),
                y = ReadSingle(),
            };
        }

        public Vector3 ReadVector3()
        {
            return new Vector3
            {
                x = ReadSingle(),
                y = ReadSingle(),
                z = ReadSingle(),
            };
        }

        public Vector4 ReadVector4()
        {
            return new Vector4
            {
                x = ReadSingle(),
                y = ReadSingle(),
                z = ReadSingle(),
                w = ReadSingle(),
            };
        }

        public Quaternion ReadQuaternion()
        {
            return new Quaternion
            {
                x = ReadSingle(),
                y = ReadSingle(),
                z = ReadSingle(),
                w = ReadSingle(),
            };
        }

        public Vector2? ReadVector2Optional()
        {
            bool hasValue = ReadBoolean();
            if (!hasValue)
                return null;

            return ReadVector2();
        }

        public Vector3? ReadVector3Optional()
        {
            bool hasValue = ReadBoolean();
            if (!hasValue)
                return null;

            return ReadVector3();
        }

        public Vector4? ReadVector4Optional()
        {
            bool hasValue = ReadBoolean();
            if (!hasValue)
                return null;

            return ReadVector4();
        }

        public Quaternion? ReadQuaternionOptional()
        {
            bool hasValue = ReadBoolean();
            if (!hasValue)
                return null;

            return ReadQuaternion();
        }

        public int[] ReadIntArray()
        {
            int len = ReadInt32();
            var o = new int[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = ReadInt32();
            }

            return o;
        }

        public float[] ReadSingleArray()
        {
            int len = ReadInt32();
            var o = new float[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = ReadSingle();
            }

            return o;
        }

        public bool[] ReadBoolArray()
        {
            int len = ReadInt32();
            var o = new bool[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = ReadBoolean();
            }

            return o;
        }

        public BitArray ReadBitArray()
        {
            return new BitArray(ReadBytes(ReadInt32()));
        }

        public ActorPacket ReadActorPacket()
        {
            return new ActorPacket
            {
                ID = ReadInt32(),
                Name = ReadString(),
                SteamID = ReadString(),
                Position = ReadVector3(),
                FacingDirection = ReadVector3(),
                Flags = ReadActorFlags(),
                JSONSkin = ReadVillager(),
            };
        }

        public ActorStateFlags ReadActorFlags()
        {
            return new ActorStateFlags
            {
                Disconnected = ReadBoolean(),
                Dead = ReadBoolean(),
            };
        }

        public string ReadVillager()
        {
            System.Random random = new System.Random();

            return JsonUtility.ToJson(new CustomVillager());
        }

        public GameStatePacket ReadGameStatePacket()
        {
            return new GameStatePacket
            {
                ID = ReadInt32(),
                Name = ReadString(),
                EruptionTrigger = ReadBoolean(),
            };
        }

        public Packet ReadPacket()
        {
            return new Packet
            {
                ID = (PacketType) ReadInt32(),
                Sender = new Guid(ReadBytes(16)),
                Data = ReadBytes(ReadInt32()),
            };
        }
    }
}
