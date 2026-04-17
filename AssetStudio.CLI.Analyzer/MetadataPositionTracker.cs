using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// Metadata 各部分的位置信息
    /// </summary>
    public class MetadataPartInfo
    {
        /// <summary>
        /// 部分名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 非资源数据类型
        /// </summary>
        public NonResourceType NonResourceType { get; set; }

        /// <summary>
        /// 起始位置（在 SerializedFile stream 中）
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        /// 结束位置（在 SerializedFile stream 中）
        /// </summary>
        public long EndPosition { get; set; }

        /// <summary>
        /// 大小（字节）
        /// </summary>
        public long Size => EndPosition - StartPosition;
    }

    /// <summary>
    /// SerializedFile Metadata 位置追踪器
    /// 用于追踪 SerializedFile 中各 Metadata 部分的精确位置
    /// </summary>
    public class MetadataPositionTracker
    {
        private SerializedFileFormatVersion _version; // 非 readonly 以支持赋值
        private EndianBinaryReader _reader; // 非 readonly 以支持 FileReader 赋值
        private readonly List<MetadataPartInfo> _parts = new List<MetadataPartInfo>();

        // 反射获取 SerializedFile 的私有字段
        private static readonly FieldInfo? HeaderField;
        private static readonly FieldInfo? TypesField;
        private static readonly FieldInfo? ObjectsField;
        private static readonly FieldInfo? ScriptTypesField;
        private static readonly FieldInfo? ExternalsField;
        private static readonly FieldInfo? RefTypesField;
        private static readonly MethodInfo? SetVersionMethod;
        private static readonly FieldInfo? ReaderField; // SerializedFile.reader

        static MetadataPositionTracker()
        {
            var serializedFileType = typeof(SerializedFile);
            HeaderField = serializedFileType.GetField("header", BindingFlags.Public | BindingFlags.Instance);
            TypesField = serializedFileType.GetField("m_Types", BindingFlags.Public | BindingFlags.Instance);
            ObjectsField = serializedFileType.GetField("m_Objects", BindingFlags.NonPublic | BindingFlags.Instance);
            ScriptTypesField = serializedFileType.GetField("m_ScriptTypes", BindingFlags.NonPublic | BindingFlags.Instance);
            ExternalsField = serializedFileType.GetField("m_Externals", BindingFlags.Public | BindingFlags.Instance);
            RefTypesField = serializedFileType.GetField("m_RefTypes", BindingFlags.NonPublic | BindingFlags.Instance);
            SetVersionMethod = serializedFileType.GetMethod("SetVersion", BindingFlags.Public | BindingFlags.Instance);
            ReaderField = serializedFileType.GetField("reader", BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// 通过反射获取 SerializedFile 的 reader
        /// </summary>
        public static FileReader? GetSerializedFileReader(SerializedFile serializedFile)
        {
            if (ReaderField == null) return null;
            return ReaderField.GetValue(serializedFile) as FileReader;
        }

        /// <summary>
        /// 通过反射获取 SerializedFile 的 reader stream（已废弃，请使用 GetSerializedFileReader）
        /// </summary>
        [Obsolete("Use GetSerializedFileReader instead")]
        public static Stream? GetSerializedFileStream(SerializedFile serializedFile)
        {
            if (ReaderField == null) return null;
            var reader = ReaderField.GetValue(serializedFile) as FileReader;
            return reader?.BaseStream;
        }

        /// <summary>
        /// 构造函数（使用 FileReader）
        /// </summary>
        /// <param name="reader">SerializedFile 的 FileReader</param>
        /// <param name="fileName">文件名</param>
        public MetadataPositionTracker(FileReader reader, string fileName)
        {
            // 使用传入的 FileReader，它会继承正确的 Endian 设置
            reader.Position = 0;
            _reader = reader;
        }

        /// <summary>
        /// 构造函数（使用 Stream，已废弃）
        /// </summary>
        [Obsolete("Use FileReader constructor instead")]
        public MetadataPositionTracker(Stream stream, string fileName)
        {
            stream.Position = 0;
            // SerializedFile header 始终使用 LittleEndian
            _reader = new EndianBinaryReader(stream, EndianType.LittleEndian);
        }

        /// <summary>
        /// 追踪结果
        /// </summary>
        public List<MetadataPartInfo> Parts => _parts;

        /// <summary>
        /// 追踪 Metadata 各部分位置
        /// 返回所有部分的边界信息
        /// </summary>
        public void Track(bool verbose = false)
        {
            _parts.Clear();
            _verbose = verbose;

            // Part 1: Header + Version/Platform
            TrackHeaderAndVersion();

            // Part 2: Types + TypeTree
            TrackTypes();

            // Part 3: ObjectDir
            TrackObjectDir();

            // Part 4: Externals (ScriptTypes + Externals + RefTypes + UserInfo)
            TrackExternals();
        }

        private bool _verbose = false;

        /// <summary>
        /// 获取 Header 大小（Bundle 级）
        /// </summary>
        public static long CalculateBundleHeaderSize(BundleFile.Header header)
        {
            long size = 0;
            size += (header.signature?.Length ?? 0) + 1; // signature + null
            size += 4; // version (uint32)
            size += (header.unityVersion?.Length ?? 0) + 1; // unityVersion + null
            size += (header.unityRevision?.Length ?? 0) + 1; // unityRevision + null
            size += 8; // size (int64)
            size += 4; // compressedBlocksInfoSize (uint32)
            size += 4; // uncompressedBlocksInfoSize (uint32)
            size += 4; // flags (uint32)
            return size;
        }

        /// <summary>
        /// 追踪 Header + Version/Platform 部分
        /// </summary>
        private void TrackHeaderAndVersion()
        {
            // 重要：SerializedFile header 始终使用 LittleEndian
            // 不管 endianess 字段的值是什么
            _reader.Endian = EndianType.LittleEndian;

            long startPos = _reader.Position;

            if (_verbose)
            {
                Console.WriteLine($"[TrackHeaderAndVersion] startPos={startPos}, Endian={_reader.Endian}");
                // 检查前20字节
                byte[] firstBytes = _reader.ReadBytes(20);
                Console.WriteLine($"[TrackHeaderAndVersion] 前20字节: {BitConverter.ToString(firstBytes)}");
                _reader.Position = startPos;
            }

            // Read Header
            uint metadataSize = _reader.ReadUInt32();
            long fileSize = _reader.ReadUInt32();
            _version = (SerializedFileFormatVersion)_reader.ReadUInt32();
            long dataOffset = _reader.ReadUInt32();

            if (_verbose)
            {
                Console.WriteLine($"[Header] metadataSize={metadataSize}, fileSize={fileSize}, version={_version}, dataOffset={dataOffset}");
            }

            if (_version >= SerializedFileFormatVersion.Unknown_9)
            {
                byte endianess = _reader.ReadByte();
                _reader.ReadBytes(3); // reserved
                _reader.Endian = endianess == 0 ? EndianType.LittleEndian : EndianType.BigEndian;
            }

            if (_version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                metadataSize = _reader.ReadUInt32();
                fileSize = _reader.ReadInt64();
                dataOffset = _reader.ReadInt64();
                _reader.ReadInt64(); // unknown
            }

            // Read Version/Platform
            if (_version >= SerializedFileFormatVersion.Unknown_7)
            {
                _reader.ReadStringToNull(); // unityVersion
            }

            if (_version >= SerializedFileFormatVersion.Unknown_8)
            {
                _reader.ReadInt32(); // m_TargetPlatform
            }

            if (_version >= SerializedFileFormatVersion.HasTypeTreeHashes)
            {
                _reader.ReadBoolean(); // m_EnableTypeTree
            }

            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "Header",
                NonResourceType = NonResourceType.FileHeader,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪 Types + TypeTree 部分
        /// </summary>
        private void TrackTypes()
        {
            long startPos = _reader.Position;

            int typeCount = _reader.ReadInt32();

            for (int i = 0; i < typeCount; i++)
            {
                TrackSingleType();
            }

            // bigIDEnabled (version 7-13)
            if (_version >= SerializedFileFormatVersion.Unknown_7 && _version < SerializedFileFormatVersion.Unknown_14)
            {
                _reader.ReadInt32();
            }

            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "Types",
                NonResourceType = NonResourceType.TypeTree,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪单个 SerializedType
        /// </summary>
        private void TrackSingleType()
        {
            _reader.ReadInt32(); // classID

            if (_version >= SerializedFileFormatVersion.RefactoredClassId)
            {
                _reader.ReadBoolean(); // m_IsStrippedType
            }

            if (_version >= SerializedFileFormatVersion.RefactorTypeData)
            {
                _reader.ReadInt16(); // m_ScriptTypeIndex
            }

            // Hash fields
            if (_version >= SerializedFileFormatVersion.HasTypeTreeHashes)
            {
                // ScriptID (conditional)
                bool needScriptID = (_version >= SerializedFileFormatVersion.RefactoredClassId);
                if (needScriptID)
                {
                    // Check if classID == 114 (MonoBehaviour)
                    // This requires knowing the classID value, simplified here
                }
                _reader.ReadBytes(16); // m_OldTypeHash
            }

            // TypeTree (if enabled)
            if (_version >= SerializedFileFormatVersion.Unknown_12 || _version == SerializedFileFormatVersion.Unknown_10)
            {
                TrackTypeTreeBlob();
            }
            else if (_version >= SerializedFileFormatVersion.HasTypeTreeHashes)
            {
                TrackTypeTreeLegacy();
            }

            // TypeDependencies
            if (_version >= SerializedFileFormatVersion.StoresTypeDependencies)
            {
                _reader.ReadInt32Array();
            }
        }

        /// <summary>
        /// 追踪 TypeTree Blob 格式（新版本）
        /// </summary>
        private void TrackTypeTreeBlob()
        {
            int numberOfNodes = _reader.ReadInt32();
            int stringBufferSize = _reader.ReadInt32();

            // Node bytes per node depends on version
            // 每个 node: version(2) + level(1) + flags(1) + typeOffset(4) + nameOffset(4) + byteSize(4) + index(4) + metaFlag(4)
            int nodeSize = 24; // Default (不含 RefTypeHash)
            if (_version >= SerializedFileFormatVersion.TypeTreeNodeWithTypeFlags)
            {
                nodeSize = 32; // Includes RefTypeHash (8 bytes)
            }

            _reader.Position += numberOfNodes * nodeSize;
            _reader.ReadBytes(stringBufferSize);
        }

        /// <summary>
        /// 追踪 TypeTree Legacy 格式（旧版本）
        /// </summary>
        private void TrackTypeTreeLegacy()
        {
            // Recursive reading, simplified estimation
            // Each node: type(string) + name(string) + size(4) + index(4) + typeflags(4) + version(4) + metaflag(4) + childrenCount(4)
            TrackTypeTreeNodeRecursive(0);
        }

        /// <summary>
        /// 递归追踪 TypeTree 节点
        /// </summary>
        private void TrackTypeTreeNodeRecursive(int level)
        {
            _reader.ReadStringToNull(); // type
            _reader.ReadStringToNull(); // name
            _reader.ReadInt32(); // byteSize

            if (_version == SerializedFileFormatVersion.Unknown_2)
            {
                _reader.ReadInt32(); // variableCount
            }

            if (_version != SerializedFileFormatVersion.Unknown_3)
            {
                _reader.ReadInt32(); // index
            }

            _reader.ReadInt32(); // typeFlags
            _reader.ReadInt32(); // version

            if (_version != SerializedFileFormatVersion.Unknown_3)
            {
                _reader.ReadInt32(); // metaFlag
            }

            int childrenCount = _reader.ReadInt32();
            for (int i = 0; i < childrenCount; i++)
            {
                TrackTypeTreeNodeRecursive(level + 1);
            }
        }

        /// <summary>
        /// 追踪 ObjectDir 部分
        /// </summary>
        private void TrackObjectDir()
        {
            long startPos = _reader.Position;

            int objectCount = _reader.ReadInt32();

            for (int i = 0; i < objectCount; i++)
            {
                TrackSingleObjectInfo();
            }

            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "ObjectDir",
                NonResourceType = NonResourceType.ObjectDir,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪单个 ObjectInfo
        /// </summary>
        private void TrackSingleObjectInfo()
        {
            // PathID
            if (_version >= SerializedFileFormatVersion.Unknown_7 && _version < SerializedFileFormatVersion.Unknown_14)
            {
                // bigIDEnabled handling - simplified, assume not enabled
            }

            if (_version >= SerializedFileFormatVersion.Unknown_14)
            {
                _reader.AlignStream();
                _reader.ReadInt64(); // PathID
            }
            else if (_version < SerializedFileFormatVersion.Unknown_14)
            {
                _reader.ReadInt32(); // PathID (old format)
            }
            else
            {
                _reader.ReadInt64(); // PathID (bigIDEnabled)
            }

            // byteStart
            if (_version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                _reader.ReadInt64();
            }
            else
            {
                _reader.ReadUInt32();
            }

            // byteSize
            _reader.ReadUInt32();

            // typeID
            _reader.ReadInt32();

            // classID / serializedType ref
            if (_version < SerializedFileFormatVersion.RefactoredClassId)
            {
                _reader.ReadUInt16();
            }
            else
            {
                // typeID references m_Types[index]
            }

            // isDestroyed / stripped flags
            if (_version < SerializedFileFormatVersion.HasScriptTypeIndex)
            {
                _reader.ReadUInt16();
            }

            if (_version >= SerializedFileFormatVersion.HasScriptTypeIndex && _version < SerializedFileFormatVersion.RefactorTypeData)
            {
                _reader.ReadInt16();
            }

            if (_version == SerializedFileFormatVersion.SupportsStrippedObject || _version == SerializedFileFormatVersion.RefactoredClassId)
            {
                _reader.ReadByte();
            }
        }

        /// <summary>
        /// 追踪 Externals 部分（拆分为四个细分部分）
        /// </summary>
        private void TrackExternals()
        {
            long externalsStartPos = _reader.Position;

            // 追踪四个细分部分
            TrackScriptTypes();
            TrackFileIdentifier();
            TrackRefTypes();
            TrackUserInformation();

            // 添加汇总的 Externals 条目（四个部分累加）保持向后兼容
            long externalsEndPos = _reader.Position;
            _parts.Add(new MetadataPartInfo
            {
                Name = "Externals",
                NonResourceType = NonResourceType.Externals,
                StartPosition = externalsStartPos,
                EndPosition = externalsEndPos
            });
        }

        /// <summary>
        /// 追踪 ScriptTypes 部分
        /// </summary>
        private void TrackScriptTypes()
        {
            if (_version < SerializedFileFormatVersion.HasScriptTypeIndex)
            {
                // 版本不支持 ScriptTypes，不添加条目
                return;
            }

            long startPos = _reader.Position;
            int scriptCount = _reader.ReadInt32();
            for (int i = 0; i < scriptCount; i++)
            {
                _reader.ReadInt32(); // localSerializedFileIndex
                if (_version < SerializedFileFormatVersion.Unknown_14)
                {
                    _reader.ReadInt32();
                }
                else
                {
                    _reader.AlignStream();
                    _reader.ReadInt64();
                }
            }
            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "ScriptTypes",
                NonResourceType = NonResourceType.ScriptTypes,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪 FileIdentifier 部分（真正的 Externals）
        /// </summary>
        private void TrackFileIdentifier()
        {
            long startPos = _reader.Position;
            int externalsCount = _reader.ReadInt32();
            for (int i = 0; i < externalsCount; i++)
            {
                if (_version >= SerializedFileFormatVersion.Unknown_6)
                {
                    _reader.ReadStringToNull(); // tempEmpty (空字符串)
                }
                if (_version >= SerializedFileFormatVersion.Unknown_5)
                {
                    _reader.ReadBytes(16); // guid
                    _reader.ReadInt32(); // type
                }
                _reader.ReadStringToNull(); // pathName
            }
            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "FileIdentifier",
                NonResourceType = NonResourceType.Externals, // 使用 Externals 类型，因为这是真正的 External 引用
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪 RefTypes 部分
        /// </summary>
        private void TrackRefTypes()
        {
            if (_version < SerializedFileFormatVersion.SupportsRefObject)
            {
                // 版本不支持 RefTypes，不添加条目
                return;
            }

            long startPos = _reader.Position;
            int refTypesCount = _reader.ReadInt32();
            for (int i = 0; i < refTypesCount; i++)
            {
                TrackSingleRefType();
            }
            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "RefTypes",
                NonResourceType = NonResourceType.RefTypes,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪 UserInformation 部分
        /// </summary>
        private void TrackUserInformation()
        {
            if (_version < SerializedFileFormatVersion.Unknown_5)
            {
                // 版本不支持 UserInformation，不添加条目
                return;
            }

            long startPos = _reader.Position;
            _reader.ReadStringToNull();
            long endPos = _reader.Position;

            _parts.Add(new MetadataPartInfo
            {
                Name = "UserInformation",
                NonResourceType = NonResourceType.UserInformation,
                StartPosition = startPos,
                EndPosition = endPos
            });
        }

        /// <summary>
        /// 追踪单个 RefType
        /// </summary>
        private void TrackSingleRefType()
        {
            TrackSingleType(); // Same structure as regular Type

            // Additional fields for RefType
            if (_version >= SerializedFileFormatVersion.StoresTypeDependencies)
            {
                _reader.ReadStringToNull(); // m_KlassName
                _reader.ReadStringToNull(); // m_NameSpace
                _reader.ReadStringToNull(); // m_AsmName
            }
        }
    }
}