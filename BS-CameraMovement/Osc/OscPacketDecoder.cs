using System;
using System.Collections.Generic;

namespace BS_CameraMovement.Osc
{
    /// <summary>
    /// 解析済みOSCメッセージ
    /// </summary>
    public class OscMessage
    {
        public string Address { get; private set; }
        public List<object> Arguments { get; private set; }

        public OscMessage(string address, List<object> arguments)
        {
            Address = address;
            Arguments = arguments;
        }

        public int GetInt(int index) => (int)Arguments[index];
        public float GetFloat(int index) => (float)Arguments[index];
        public string GetString(int index) => (string)Arguments[index];
    }

    /// <summary>
    /// OSCパケットのデコーダー
    /// </summary>
    internal static class OscPacketDecoder
    {
        /// <summary>
        /// 受信バイトデータからOscMessageを解析する
        /// </summary>
        public static OscMessage Decode(Byte[] buffer, int length)
        {
            if (length < 4) return null;

            var offset = 0;

            // アドレスパターンの読み取り
            var address = OscDataTypes.ReadString(buffer, offset);
            offset += OscDataTypes.GetStringSize(buffer, offset);

            if (offset >= length) return new OscMessage(address, new List<object>());

            // タイプタグ文字列の読み取り（","で始まる）
            var typeTags = OscDataTypes.ReadString(buffer, offset);
            offset += OscDataTypes.GetStringSize(buffer, offset);

            if (typeTags.Length == 0 || typeTags[0] != ',')
                return new OscMessage(address, new List<object>());

            // 引数の解析
            var arguments = new List<object>();
            for (var i = 1; i < typeTags.Length; i++)
            {
                if (offset >= length) break;

                var tag = typeTags[i];
                if (!OscDataTypes.IsSupportedTag(tag)) continue;

                switch (tag)
                {
                    case 'i':
                        arguments.Add(OscDataTypes.ReadInt(buffer, offset));
                        offset += 4;
                        break;
                    case 'f':
                        arguments.Add(OscDataTypes.ReadFloat(buffer, offset));
                        offset += 4;
                        break;
                    case 's':
                        arguments.Add(OscDataTypes.ReadString(buffer, offset));
                        offset += OscDataTypes.GetStringSize(buffer, offset);
                        break;
                    case 'b':
                        var blobSize = OscDataTypes.ReadInt(buffer, offset);
                        offset += 4;
                        var blob = new Byte[blobSize];
                        System.Buffer.BlockCopy(buffer, offset, blob, 0, blobSize);
                        arguments.Add(blob);
                        offset += OscDataTypes.Align4(blobSize);
                        break;
                }
            }

            return new OscMessage(address, arguments);
        }
    }
}
