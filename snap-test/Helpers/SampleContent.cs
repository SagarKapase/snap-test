using System.IO.Compression;
using System.Text;

namespace snap_test.Helpers
{
    /// <summary>
    /// Hardcoded / procedurally generated sample payloads shared by the Formats and Files controllers.
    /// Everything is deterministic: the same request always yields the same bytes.
    /// </summary>
    public static class SampleContent
    {
        // -------------------- TEXT SAMPLES --------------------
        public const string Text =
            "TestingAPIs sample text file\n" +
            "=======================\n\n" +
            "This is plain text served for testing HTTP clients.\n" +
            "Line 4: The quick brown fox jumps over the lazy dog.\n" +
            "Line 5: 0123456789 !@#$%^&*()\n";

        public const string Csv =
            "id,name,email,department,salary,active\n" +
            "1,Alice Johnson,alice@example.com,Engineering,98000,true\n" +
            "2,Bob Smith,bob@example.com,Marketing,72000,true\n" +
            "3,\"Carol, Jr.\",carol@example.com,Sales,65000,false\n" +
            "4,\"David \"\"Dave\"\" Lee\",david@example.com,Engineering,105000,true\n" +
            "5,Eve Martinez,eve@example.com,Finance,81000,true\n";

        public const string Tsv =
            "id\tname\temail\tdepartment\tsalary\tactive\n" +
            "1\tAlice Johnson\talice@example.com\tEngineering\t98000\ttrue\n" +
            "2\tBob Smith\tbob@example.com\tMarketing\t72000\ttrue\n" +
            "3\tCarol White\tcarol@example.com\tSales\t65000\tfalse\n" +
            "4\tDavid Lee\tdavid@example.com\tEngineering\t105000\ttrue\n" +
            "5\tEve Martinez\teve@example.com\tFinance\t81000\ttrue\n";

        public const string Json =
            "{\n" +
            "  \"id\": 1,\n" +
            "  \"name\": \"Alice Johnson\",\n" +
            "  \"email\": \"alice@example.com\",\n" +
            "  \"roles\": [\"admin\", \"editor\"],\n" +
            "  \"active\": true,\n" +
            "  \"address\": { \"city\": \"Pune\", \"country\": \"India\", \"zip\": \"411001\" },\n" +
            "  \"createdAt\": \"2025-06-12T10:30:00Z\"\n" +
            "}\n";

        public const string Xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<users>\n" +
            "  <user id=\"1\">\n" +
            "    <name>Alice Johnson</name>\n" +
            "    <email>alice@example.com</email>\n" +
            "    <roles><role>admin</role><role>editor</role></roles>\n" +
            "    <active>true</active>\n" +
            "  </user>\n" +
            "  <user id=\"2\">\n" +
            "    <name>Bob Smith</name>\n" +
            "    <email>bob@example.com</email>\n" +
            "    <roles><role>viewer</role></roles>\n" +
            "    <active>false</active>\n" +
            "  </user>\n" +
            "</users>\n";

        // 1x1 transparent GIF89a (well-known valid bytes).
        public static readonly byte[] Gif = Convert.FromBase64String("R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        // -------------------- PNG ENCODER --------------------
        /// <summary>Encodes a real RGB PNG: "gradient" (default) or "checkerboard".</summary>
        public static byte[] Png(int size, string pattern = "gradient")
        {
            size = Math.Clamp(size, 1, 512);
            var checker = string.Equals(pattern, "checkerboard", StringComparison.OrdinalIgnoreCase);

            // Raw scanlines: filter byte 0 + RGB triplets.
            var raw = new byte[size * (size * 3 + 1)];
            var i = 0;
            for (var y = 0; y < size; y++)
            {
                raw[i++] = 0;
                for (var x = 0; x < size; x++)
                {
                    if (checker)
                    {
                        var cell = Math.Max(1, size / 8);
                        byte v = ((x / cell + y / cell) % 2 == 0) ? (byte)255 : (byte)30;
                        raw[i++] = v; raw[i++] = v; raw[i++] = (byte)(v == 255 ? 0 : 30); // yellow/black "bee" squares
                    }
                    else
                    {
                        raw[i++] = (byte)(size == 1 ? 255 : x * 255 / (size - 1));
                        raw[i++] = (byte)(size == 1 ? 180 : y * 255 / (size - 1));
                        raw[i++] = 128;
                    }
                }
            }

            byte[] idat;
            using (var ms = new MemoryStream())
            {
                using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    z.Write(raw, 0, raw.Length);
                idat = ms.ToArray();
            }

            var ihdr = new byte[13];
            WriteBigEndian(ihdr, 0, (uint)size);
            WriteBigEndian(ihdr, 4, (uint)size);
            ihdr[8] = 8;  // bit depth
            ihdr[9] = 2;  // color type: truecolor RGB
            ihdr[10] = 0; // compression
            ihdr[11] = 0; // filter
            ihdr[12] = 0; // interlace

            using var png = new MemoryStream();
            png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            WriteChunk(png, "IHDR", ihdr);
            WriteChunk(png, "IDAT", idat);
            WriteChunk(png, "IEND", Array.Empty<byte>());
            return png.ToArray();
        }

        private static void WriteChunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            WriteBigEndian(len, 0, (uint)data.Length);
            s.Write(len);

            var typeBytes = Encoding.ASCII.GetBytes(type);
            s.Write(typeBytes);
            s.Write(data);

            var crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, crc);
            s.Write(crcBytes);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static uint Crc32(params byte[][] parts)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var part in parts)
                foreach (var b in part)
                    crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        // -------------------- PDF BUILDER --------------------
        /// <summary>Minimal valid single-page PDF 1.4 with xref offsets computed from the actual bytes.</summary>
        public static byte[] Pdf(string title = "TestingAPIs Sample PDF")
        {
            string Escape(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

            var stream =
                "BT /F1 24 Tf 72 720 Td (" + Escape(title) + ") Tj ET\n" +
                "BT /F1 12 Tf 72 690 Td (Hardcoded PDF generated for testing HTTP clients.) Tj ET\n" +
                "BT /F1 12 Tf 72 672 Td (The quick brown fox jumps over the lazy dog.) Tj ET\n";

            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                "<< /Length " + Encoding.ASCII.GetByteCount(stream) + " >>\nstream\n" + stream + "endstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Title (" + Escape(title) + ") /Producer (TestingAPIs) >>"
            };

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var n = 0; n < objects.Length; n++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
                sb.Append(n + 1).Append(" 0 obj\n").Append(objects[n]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append("xref\n0 ").Append(objects.Length + 1).Append('\n');
            sb.Append("0000000000 65535 f \n");
            foreach (var off in offsets)
                sb.Append(off.ToString("D10")).Append(" 00000 n \n");
            sb.Append("trailer\n<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R /Info 6 0 R >>\n");
            sb.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        // -------------------- ZIP BUILDER --------------------
        /// <summary>In-memory ZIP containing the text/csv/json/xml samples plus a PNG.</summary>
        public static byte[] Zip()
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                void Add(string name, byte[] content)
                {
                    var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                    entry.LastWriteTime = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
                    using var es = entry.Open();
                    es.Write(content);
                }

                Add("sample.txt", Encoding.UTF8.GetBytes(Text));
                Add("sample.csv", Encoding.UTF8.GetBytes(Csv));
                Add("sample.json", Encoding.UTF8.GetBytes(Json));
                Add("data/sample.xml", Encoding.UTF8.GetBytes(Xml));
                Add("images/sample.png", Png(64));
            }
            return ms.ToArray();
        }

        // -------------------- DETERMINISTIC BYTES --------------------
        /// <summary>Fills a buffer with seeded pseudo-random bytes (same seed + length = same bytes).</summary>
        public static byte[] SeededBytes(int length, int seed)
        {
            var bytes = new byte[length];
            new Random(seed).NextBytes(bytes);
            return bytes;
        }
    }
}
