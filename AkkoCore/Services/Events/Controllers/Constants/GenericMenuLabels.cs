//using DSharpPlus;
//using DSharpPlus.Entities;
//using System;

//namespace AkkoCore.Services.Events.Controllers.Constants
//{
//    public static class GenericMenuLabels
//    {
//        public const string SelectAnOptionLabel = "menuopt_selectanoption_desc";

//        public const string ExitButtonId = "button_exit_end";
//        public const string ExitButtonLabel = "button_exit_label";

//        public const string EnableId = "menuopt_enable_id";
//        public const string EnableLabel = "menuopt_enable_desc";

//        public const string DisableId = "menuopt_disable_id";
//        public const string DisableLabel = "menuopt_disable_desc";

//        #region Colors
//        public const string RedId = "menuopt_red_id";
//        public const string BlueId = "menuopt_blue_id";
//        public const string GreenId = "menuopt_green_id";

//        public const string RedLabel = "menuopt_red_desc";
//        public const string BlueLabel = "menuopt_blue_desc";
//        public const string GreenLabel = "menuopt_green_desc";
//        #endregion Colors

//        public static bool IsBoolean(string componentId)
//            => EnableId.Equals(componentId, StringComparison.Ordinal) || DisableId.Equals(componentId, StringComparison.Ordinal);

//        public static bool IsColor(string componentId)
//        {
//            return RedId.Equals(componentId, StringComparison.Ordinal)
//                || BlueId.Equals(componentId, StringComparison.Ordinal)
//                || GreenId.Equals(componentId, StringComparison.Ordinal);
//        }
//    }
//}