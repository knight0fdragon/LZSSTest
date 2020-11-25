using System.Collections.Generic;
using System.Linq;
//Original source located at: https://github.com/opensource-apple/kext_tools/blob/master/compression.c
namespace LZSSTest
{

    public class LZSS
    {
        const int BufferSize = 1 << 10;
        const int DictionarySize = 34;


        public class EncodeState
        {
            public int[] lchild = new int[BufferSize + 1];
            public int[] rchild = Enumerable.Repeat(BufferSize, BufferSize + 258).ToArray();
            public int[] parent = Enumerable.Repeat(BufferSize, BufferSize + 1).ToArray();
            
            public byte[] text_buf = Enumerable.Repeat((byte)0xFF, BufferSize + DictionarySize + 1).ToArray();

            public int match_position = 0;
            public int match_length = 0;
        };

        static void insert_node(EncodeState sp, int r)
        {
            
            int cmp = 1;
            int p = BufferSize + 1 + sp.text_buf[r];
            sp.rchild[r] = sp.lchild[r] = BufferSize;
            sp.match_length = 0;
            for (; ; )
            {
                if (cmp >= 0)
                {
                    if (sp.rchild[p] != BufferSize)
                        p = sp.rchild[p];
                    else
                    {
                        sp.rchild[p] = r;
                        sp.parent[r] = p;
                        return;
                    }
                }
                else
                {
                    if (sp.lchild[p] != BufferSize)
                        p = sp.lchild[p];
                    else
                    {
                        sp.lchild[p] = r;
                        sp.parent[r] = p;
                        return;
                    }
                }
                var i = 0;
                for (i = 1; i < DictionarySize; i++)
                {
                    if ((cmp = sp.text_buf[r + i] - sp.text_buf[p + i]) != 0)
                        break;
                }
                
                
                if (i > sp.match_length)
                {
                    sp.match_position = p;
                    if ((sp.match_length = i) >= DictionarySize)
                        break;
                }
            }
            sp.parent[r] = sp.parent[p];
            sp.lchild[r] = sp.lchild[p];
            sp.rchild[r] = sp.rchild[p];
            sp.parent[sp.lchild[p]] = r;
            sp.parent[sp.rchild[p]] = r;
            if (sp.rchild[sp.parent[p]] == p)
                sp.rchild[sp.parent[p]] = r;
            else
                sp.lchild[sp.parent[p]] = r;
            sp.parent[p] = BufferSize;  /* remove p */
        }
        /* deletes node p from tree */
        static void delete_node(EncodeState sp, int p)
        {
            int q;
            if (sp.parent[p] == BufferSize)
                return;  /* not in tree */
            if (sp.rchild[p] == BufferSize)
                q = sp.lchild[p];
            else if (sp.lchild[p] == BufferSize)
                q = sp.rchild[p];
            else
            {
                q = sp.lchild[p];
                if (sp.rchild[q] != BufferSize)
                {
                    do
                    {
                        q = sp.rchild[q];
                    } while (sp.rchild[q] != BufferSize);
                    sp.rchild[sp.parent[q]] = sp.lchild[q];
                    sp.parent[sp.lchild[q]] = sp.parent[q];
                    sp.lchild[q] = sp.lchild[p];
                    sp.parent[sp.lchild[p]] = q;
                }
                sp.rchild[q] = sp.rchild[p];
                sp.parent[sp.rchild[p]] = q;
            }
            sp.parent[q] = sp.parent[p];
            if (sp.rchild[sp.parent[p]] == p)
                sp.rchild[sp.parent[p]] = q;
            else
                sp.lchild[sp.parent[p]] = q;
            sp.parent[p] = BufferSize;
        }
        public static List<byte> Compress(List<byte> input)
        {
            var output = new List<byte>();
            const int THRESHOLD = 2;
            EncodeState sp = new EncodeState();
            int i;
            byte c;
            int len, last_match_length;
            byte[] code_buf = new byte[THRESHOLD * 8 + 1];
            byte mask = 1;
            code_buf[0] = 0;
            int code_buf_ptr = 1;
            int s = 0;
            int r = BufferSize - DictionarySize;
            int inputIdx = 0;
            for (len = 0; len < DictionarySize && inputIdx < input.Count; len++)
                sp.text_buf[r + len] = input[inputIdx++];
            for (i = 1; i <= DictionarySize; i++)
                insert_node(sp, r - i);
            insert_node(sp, r);
            do
            {
                if (sp.match_length > len)
                    sp.match_length = len;
                if (sp.match_length <= THRESHOLD)
                {
                    sp.match_length = 1;  /* Not long enough match.  Send one byte. */
                    code_buf[0] |= mask;  /* 'send one byte' flag */
                    code_buf[code_buf_ptr++] = sp.text_buf[r];  /* Send uncoded. */
                }
                else
                {
                    code_buf[code_buf_ptr++] = (byte)sp.match_position;
                    var high = ((sp.match_position & 0xFF00) >> 3);
                    var low = ((sp.match_length - (THRESHOLD + 1)) & 0x1F);
                    code_buf[code_buf_ptr++] = (byte)(high | low);
                }
                if ((mask <<= 1) == 0)
                {
                    for (i = 0; i < code_buf_ptr; i++)
                    {
                        output.Add(code_buf[i]);
                    }
                    for (i = 0; i < code_buf.Length; i++)
                    {
                        code_buf[i] = 0;
                    }
                    code_buf_ptr = mask = 1;

                }
                

                last_match_length = sp.match_length;
                for (i = 0; i < last_match_length && inputIdx < input.Count; i++)
                {
                    delete_node(sp, s);    /* Delete old strings and */
                    c = input[inputIdx++];
                    sp.text_buf[s] = c;    /* read new bytes */
                    if (s < (DictionarySize - 1))
                        sp.text_buf[s + BufferSize] = c;
                    s = (s + 1) & (BufferSize - 1);
                    r = (r + 1) & (BufferSize - 1);
                    insert_node(sp, r);
                }
                while (i++ < last_match_length)
                {
                    delete_node(sp, s);
                    s = (s + 1) & (BufferSize - 1);
                    r = (r + 1) & (BufferSize - 1);
                    if ((--len) == 0)
                        insert_node(sp, r);
                }
            } while (len > 0);
            if (code_buf_ptr > 1)
            {    /* Send remaining code. */
                for (i = 0; i < code_buf_ptr; i++)
                {
                    output.Add(code_buf[i]);
                }
            }
            return output;
        }

        public static List<byte> Decompress(List<byte> input)
        {
            const int THRESHOLD = 2;
            var text_buf = new byte[BufferSize];
            int inputIdx = 0;
            var output = new List<byte>();
            int bufferIdx = BufferSize - DictionarySize; //r
            byte c = 0;
            ushort flags;


            for (int i = 0; i < BufferSize - DictionarySize; i++)
                text_buf[i] = 0x0;

            flags = 0;
            for (; ; )
            {
                if (((flags >>= 1) & 0x100) == 0)
                {
                    if (inputIdx < input.Count) c = input[inputIdx++]; else break;
                    flags = (ushort)(c | 0xFF00);  /* uses higher byte cleverly */
                }   /* to count eight */
                if ((flags & 1) > 0)
                {
                    if (inputIdx < input.Count) c = input[inputIdx++]; else break;
                    output.Add(c);
                    text_buf[bufferIdx++] = c;
                    bufferIdx &= (BufferSize - 1);
                }
                else
                {
                    int i = 0;
                    int j = 0;
                    if (inputIdx < input.Count) i = input[inputIdx++]; else break;
                    if (inputIdx < input.Count) j = input[inputIdx++]; else break;
                    i |= ((j & 0xE0) << 3);
                    j = (j & 0x1F) + THRESHOLD;
                    for (int k = 0; k <= j; k++)
                    {
                        c = text_buf[(i + k) & (BufferSize - 1)];
                        output.Add(c);
                        text_buf[bufferIdx++] = c;
                        bufferIdx &= (BufferSize - 1);
                    }
                }
            }

            return output;
        }
    }

}
