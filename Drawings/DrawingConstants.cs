using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Drawings
{
   internal class DrawingConstants
    {
        internal const string ValidDevicePattern = @"[\w]+-[\d]+";

        internal const string BlockPrefixTag = "ID";

        internal const string BlockMakeTag = "MAKE";
        internal const string BlockManufacturerAbbreviatedTag = "MFG";

        internal static readonly List<string> BlockManufacturerTags = new List<string> { BlockMakeTag, BlockManufacturerAbbreviatedTag };

        internal const string BlockModelTag = "MODEL";
        internal const string BlockPartNumberTag = "PN";

        internal static readonly List<string> BlockModelTags = new List<string> { BlockModelTag, BlockPartNumberTag };

        internal const string BlockDescriptionTag = "DESCRIPTION";
        internal const string BlockInfoTag = "DESC";

        internal static readonly List<string> BlockDescriptionTags = new List<string> { BlockDescriptionTag, BlockInfoTag };

        internal const string DxfFileExtension = ".DXF";
        internal const string DwgFileExtension = ".DWG";
    }
}
