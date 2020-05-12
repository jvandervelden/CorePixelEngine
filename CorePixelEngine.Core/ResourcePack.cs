using System;

namespace CorePixelEngine
{
	public class ResourcePack
	{
		public ResourcePack()
		{
		}

        //=============================================================
        // Resource Packs - Allows you to store files in one large 
        // scrambled file - Thanks MaGetzUb for debugging a null char in std::stringstream bug
        /*ResourceBuffer::ResourceBuffer(std::ifstream& ifs, UInt32 offset, UInt32 size)
        {
            vMemory.resize(size);
            ifs.seekg(offset); ifs.read(vMemory.data(), vMemory.size());
            setg(vMemory.data(), vMemory.data(), vMemory.data() + size);
        }

        ResourcePack::ResourcePack() { }
        ResourcePack::~ResourcePack() { baseFile.close(); }

        bool ResourcePack::AddFile(const std::string& sFile)
        {
            const std::string file = makeposix(sFile);

            if (_gfs::exists(file))
            {
                sResourceFile e;
                e.nSize = (UInt32)_gfs::file_size(file);
                e.nOffset = 0; // Unknown at this stage            
                mapFiles[file] = e;
                return true;
            }
            return false;
        }

        bool ResourcePack::LoadPack(const std::string& sFile, const std::string& sKey)
        {
            // Open the resource file
            baseFile.open(sFile, std::ifstream::binary);
            if (!baseFile.is_open()) return false;

            // 1) Read Scrambled index
            UInt32 nIndexSize = 0;
            baseFile.read((char*)&nIndexSize, sizeof(UInt32));

            std::vector<char> buffer(nIndexSize);
            for (UInt32 j = 0; j < nIndexSize; j++)
                buffer[j] = baseFile.get();

            std::vector<char> decoded = scramble(buffer, sKey);
            size_t pos = 0;
            auto read = [&decoded, &pos](char* dst, size_t size) {
                memcpy((void*)dst, (const void*)(decoded.data() + pos), size);
                pos += size;
            };

            auto get = [&read]() -> int {
                char c;
                read(&c, 1);
                return c;
            };

            // 2) Read Map
            UInt32 nMapEntries = 0;
            read((char*)&nMapEntries, sizeof(UInt32));
            for (UInt32 i = 0; i < nMapEntries; i++)
            {
                UInt32 nFilePathSize = 0;
                read((char*)&nFilePathSize, sizeof(UInt32));

                std::string sFileName(nFilePathSize, ' ');
                for (UInt32 j = 0; j < nFilePathSize; j++)
                    sFileName[j] = get();

                sResourceFile e;
                read((char*)&e.nSize, sizeof(UInt32));
                read((char*)&e.nOffset, sizeof(UInt32));
                mapFiles[sFileName] = e;
            }

            // Don't close base file! we will provide a stream
            // pointer when the file is requested
            return true;
        }

        bool ResourcePack::SavePack(const std::string& sFile, const std::string& sKey)
        {
            // Create/Overwrite the resource file
            std::ofstream ofs(sFile, std::ofstream::binary);
            if (!ofs.is_open()) return false;

            // Iterate through map
            UInt32 nIndexSize = 0; // Unknown for now
            ofs.write((char*)&nIndexSize, sizeof(UInt32));
            UInt32 nMapSize = UInt32(mapFiles.size());
            ofs.write((char*)&nMapSize, sizeof(UInt32));
            for (auto& e : mapFiles)
            {
                // Write the path of the file
                size_t nPathSize = e.first.size();
                ofs.write((char*)&nPathSize, sizeof(UInt32));
                ofs.write(e.first.c_str(), nPathSize);

                // Write the file entry properties
                ofs.write((char*)&e.second.nSize, sizeof(UInt32));
                ofs.write((char*)&e.second.nOffset, sizeof(UInt32));
            }

            // 2) Write the individual Data
            std::streampos offset = ofs.tellp();
            nIndexSize = (UInt32)offset;
            for (auto& e : mapFiles)
            {
                // Store beginning of file offset within resource pack file
                e.second.nOffset = (UInt32)offset;

                // Load the file to be added
                std::vector<byte> vBuffer(e.second.nSize);
                std::ifstream i(e.first, std::ifstream::binary);
                i.read((char*)vBuffer.data(), e.second.nSize);
                i.close();

                // Write the loaded file into resource pack file
                ofs.write((char*)vBuffer.data(), e.second.nSize);
                offset += e.second.nSize;
            }

            // 3) Scramble Index
            std::vector<char> stream;
            auto write = [&stream](const char* data, size_t size) {
                size_t sizeNow = stream.size();
                stream.resize(sizeNow + size);
                memcpy(stream.data() + sizeNow, data, size);
            };

            // Iterate through map
            write((char*)&nMapSize, sizeof(UInt32));
            for (auto& e : mapFiles)
            {
                // Write the path of the file
                size_t nPathSize = e.first.size();
                write((char*)&nPathSize, sizeof(UInt32));
                write(e.first.c_str(), nPathSize);

                // Write the file entry properties
                write((char*)&e.second.nSize, sizeof(UInt32));
                write((char*)&e.second.nOffset, sizeof(UInt32));
            }
            std::vector<char> sIndexString = scramble(stream, sKey);
            UInt32 nIndexStringLen = UInt32(sIndexString.size());
            // 4) Rewrite Map (it has been updated with offsets now)
            // at start of file
            ofs.seekp(0, std::ios::beg);
            ofs.write((char*)&nIndexStringLen, sizeof(UInt32));
            ofs.write(sIndexString.data(), nIndexStringLen);
            ofs.close();
            return true;
        }

        ResourceBuffer ResourcePack::GetFileBuffer(const std::string& sFile)
        { return ResourceBuffer(baseFile, mapFiles[sFile].nOffset, mapFiles[sFile].nSize); }

        bool ResourcePack::Loaded()
        { return baseFile.is_open(); }

        std::vector<char> ResourcePack::scramble(const std::vector<char>& data, const std::string& key)
        {
            if (key.empty()) return data;
            std::vector<char> o;
            size_t c = 0;
            for (auto s : data)    o.push_back(s ^ key[(c++) % key.size()]);
            return o;
        };

        std::string ResourcePack::makeposix(const std::string& path)
        {
            std::string o;
            for (auto s : path) o += std::string(1, s == '\\' ? '/' : s);
            return o;
        };*/

        /*// Need a couple of statics as these are singleton instances
        // read from multiple locations
        std::atomic<bool> PixelGameEngine::bAtomActive{ false };
        olc::PixelGameEngine* olc::PGEX::pge = nullptr;
        olc::PixelGameEngine* olc::Platform::ptrPGE = nullptr;
        olc::PixelGameEngine* olc::Renderer::ptrPGE = nullptr;*/
        //};
        /*
        public class ResourceBuffer : Stream
        {
            ResourceBuffer(std::ifstream &ifs, UInt32 offset, UInt32 size);
            std::vector<char> vMemory;
        };

        public class ResourcePack : public std::streambuf
        {
        public:
            ResourcePack();
        ~ResourcePack();
        bool AddFile(const std::string& sFile);
            bool LoadPack(const std::string& sFile, const std::string& sKey);
            bool SavePack(const std::string& sFile, const std::string& sKey);
            ResourceBuffer GetFileBuffer(const std::string& sFile);
            bool Loaded();
        private:
            struct sResourceFile { UInt32 nSize; UInt32 nOffset; };
        std::map<std::string, sResourceFile> mapFiles;
        std::ifstream baseFile;
        std::vector<char> scramble(const std::vector<char>& data, const std::string& key);
            std::string makeposix(const std::string& path);
        };
        */

        
    }
}
