//using AkkoCore.Common;
//using DSharpPlus;
//using DSharpPlus.Entities;
//using GML = AkkoCore.Services.Events.Controllers.Components.GenericMenuLabels;

//namespace AkkoCore.Services.Events.Controllers.Components
//{
//    public static class GenericMenuOptions
//    {
//        public static DiscordSelectComponentOption Empty { get; } = new(AkkoConstants.ValidWhitespace, "menuopt_empty", AkkoConstants.ValidWhitespace, true);

//        public static DiscordButtonComponent ExitButton { get; } = new(ButtonStyle.Danger, GML.ExitButtonId, GML.ExitButtonLabel);

//        public static DiscordSelectComponentOption Enable { get; } = new(GML.EnableId, GML.EnableId, GML.EnableLabel);

//        public static DiscordSelectComponentOption Disable { get; } = new(GML.DisableId, GML.DisableId, GML.DisableLabel);

//        public static DiscordSelectComponentOption[] Booleans { get; } = new[] { Enable, Disable };

//        public static DiscordSelectComponentOption[] Colors { get; } = new[]
//        {
//            new DiscordSelectComponentOption(GML.BlueId, GML.BlueId, GML.BlueLabel),
//            new DiscordSelectComponentOption(GML.RedId, GML.RedId, GML.RedLabel),
//            new DiscordSelectComponentOption(GML.GreenId, GML.GreenId, GML.GreenLabel),
//        };
//    }
//}