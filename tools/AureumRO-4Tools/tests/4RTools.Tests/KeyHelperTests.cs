using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace _4RTools.Tests
{
    public class KeyHelperTests
    {
        [Theory]
        [InlineData(Key.F1, Keys.F1)]
        [InlineData(Key.F12, Keys.F12)]
        [InlineData(Key.A, Keys.A)]
        [InlineData(Key.D1, Keys.D1)]
        [InlineData(Key.Insert, Keys.Insert)]
        [InlineData(Key.End, Keys.End)]
        [InlineData(Key.LeftCtrl, Keys.LControlKey)]
        [InlineData(Key.LeftShift, Keys.LShiftKey)]
        [InlineData(Key.OemPlus, Keys.Oemplus)]
        [InlineData(Key.None, Keys.None)]
        public void ToFormsKeys_MapsWpfKeysAccurately(Key wpfKey, Keys expected)
        {
            Assert.Equal(expected, KeyHelper.ToFormsKeys(wpfKey));
        }

        [Theory]
        [InlineData(Keys.F1, Key.F1)]
        [InlineData(Keys.LControlKey, Key.LeftCtrl)]
        [InlineData(Keys.Oemplus, Key.OemPlus)]
        [InlineData(Keys.End, Key.End)]
        [InlineData(Keys.None, Key.None)]
        public void FromFormsKeys_MapsWinFormsKeysAccurately(Keys formsKey, Key expected)
        {
            Assert.Equal(expected, KeyHelper.FromFormsKeys(formsKey));
        }

        [Fact]
        public void TryParseKey_AcceptsWpfAndWinFormsNames()
        {
            Key key;
            Assert.True(KeyHelper.TryParseKey("Oemplus", out key));
            Assert.Equal(Key.OemPlus, key);

            Assert.True(KeyHelper.TryParseKey("F5", out key));
            Assert.Equal(Key.F5, key);

            Assert.False(KeyHelper.TryParseKey("NotARealKeyName123", out key));
            Assert.Equal(Key.None, key);
        }

        [Fact]
        public void TryParseFormsKeys_AcceptsWpfNames()
        {
            Keys keys;
            Assert.True(KeyHelper.TryParseFormsKeys("LeftCtrl", out keys));
            Assert.Equal(Keys.LControlKey, keys);

            Assert.True(KeyHelper.TryParseFormsKeys("End", out keys));
            Assert.Equal(Keys.End, keys);

            Assert.False(KeyHelper.TryParseFormsKeys("", out keys));
        }
    }
}
