using System.Collections.Generic;

namespace CirclesBot
{
    /// <summary>
    /// This class acts kinda like queue, every parameter check removes the underlying string from the buffer so YOU dont have to keep track of it
    /// </summary>
    public struct CommandBuffer
    {
        private List<string> buffer;

        public CommandBuffer(List<string> input)
        {
            buffer = input;
        }

        public string GetRemaining()
        {
            string output = "";
            for (int i = 0; i < buffer.Count; i++)
            {
                bool isLast = i == buffer.Count - 1;

                output += buffer[i];
                if (!isLast)
                    output += "_";
            }

            return output;
        }

        public int? GetInt()
        {
            foreach (var str in buffer)
            {
                if(int.TryParse(str, out int result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }

            return null;
        }

        public double? GetDouble()
        {
            foreach (var str in buffer)
            {
                if (double.TryParse(str, out double result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }

            return null;
        }

        public string GetParameter(string param)
        {
            foreach (var str in buffer)
            {
                if (str.Contains(param))
                {
                    buffer.Remove(str);
                    return str.Remove(0, param.Length);
                }
            }

            return "";
        }

        public void Discard(string val)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i].Contains(val))
                {
                    buffer[i] = buffer[i].Trim(val.ToCharArray());
                }
            }
        }

        public bool HasParameter(string param)
        {
            if (buffer.Contains(param))
            {
                buffer.Remove(param);
                return true;
            }

            return false;
        }
    }
}
