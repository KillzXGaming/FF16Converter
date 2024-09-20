using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaToolbox.Core.Textures
{
    public interface ImageBlockFormat
    {
        uint BlockWidth { get; } 
        uint BlockHeight { get; }
        uint BlockDepth { get; }
    }
}
