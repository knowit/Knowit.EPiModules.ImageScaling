using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ImageLibrary;

namespace Knowit.EPiModules.ImageScaling.Sample.Business.ImageScaling
{
    public static class ImageOperationPresets
    {
        private static readonly List<ImageOperationPreset> Presets = new List<ImageOperationPreset>();

        static ImageOperationPresets()
        {
            Presets.Add(FixedSmall);
            Presets.Add(Fluid250);
        }

        public static List<ImageOperation> Get(ImagePreset preset)
        {
            var result = Presets.SingleOrDefault(x => String.Equals(x.Preset.ToString(), preset.ToString(), StringComparison.CurrentCultureIgnoreCase));
            return result != null ? result.ImageOperations : NoTransform.ImageOperations;
        }

        #region Presets

        private static readonly ImageOperationPreset NoTransform = new ImageOperationPreset
        {
            Preset = ImagePreset.NoTransform
        };

        private static readonly ImageOperationPreset FixedSmall = new ImageOperationPreset
        {
            Preset = ImagePreset.FixedSmall,
            ImageOperations = new List<ImageOperation>
            {
                new ImageOperation(ImageEditorCommand.ResizeKeepScale, 50, 50){ BackgroundColor = "transparent"}
            }
        };

        private static readonly ImageOperationPreset Fluid250 = new ImageOperationPreset
        {
            Preset = ImagePreset.Fluid250,
            ImageOperations = new List<ImageOperation>
            {
                new ImageOperation(ImageEditorCommand.Resize){Width = 250}
            }
        };


        #endregion

        private class ImageOperationPreset
        {
            public ImagePreset Preset { get; set; }
            public List<ImageOperation> ImageOperations { get; set; }
        }
    }
}