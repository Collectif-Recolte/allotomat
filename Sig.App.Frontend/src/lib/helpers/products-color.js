import i18n from "@/lib/i18n";
import {
  COLOR_0,
  COLOR_1,
  COLOR_2,
  COLOR_3,
  COLOR_4,
  COLOR_5,
  COLOR_6,
  COLOR_7,
  COLOR_8,
  COLOR_9,
  COLOR_10
} from "@/lib/consts/color";

const colors = [COLOR_0, COLOR_1, COLOR_2, COLOR_3, COLOR_4, COLOR_5, COLOR_6, COLOR_7, COLOR_8, COLOR_9, COLOR_10];

function getColorName(color) {
  return i18n.global.t(color);
}

function getColorBgClass(color) {
  let colorBgClass = "bg-white";
  switch (color) {
    case COLOR_0:
      colorBgClass = "bg-products-10";
      break;
    case COLOR_1:
      colorBgClass = "bg-products-50";
      break;
    case COLOR_2:
      colorBgClass = "bg-products-100";
      break;
    case COLOR_3:
      colorBgClass = "bg-products-200";
      break;
    case COLOR_4:
      colorBgClass = "bg-products-300";
      break;
    case COLOR_5:
      colorBgClass = "bg-products-400";
      break;
    case COLOR_6:
      colorBgClass = "bg-products-500";
      break;
    case COLOR_7:
      colorBgClass = "bg-products-600";
      break;
    case COLOR_8:
      colorBgClass = "bg-products-700";
      break;
    case COLOR_9:
      colorBgClass = "bg-products-800";
      break;
    case COLOR_10:
      colorBgClass = "bg-products-900";
      break;
    default:
      break;
  }
  return colorBgClass;
}

function getColorList() {
  let colorList = [];
  for (let color of colors) {
    // Hide gift card color from the available colors
    if (color !== COLOR_0) {
      colorList.push({
        value: color,
        label: getColorName(color),
        colorBgClass: getColorBgClass(color)
      });
    }
  }

  return colorList;
}

function getGiftCardBgClass(fullOpacity = false) {
  return fullOpacity ? "bg-secondary-800 bg-diagonal-pattern" : "bg-secondary-800/10 bg-diagonal-pattern";
}

function getKioskProductGroupCardClasses(color, isGiftCard = false, dark = false) {
  if (isGiftCard) {
    return {
      border: "border-secondary-800",
      bg: getGiftCardBgClass(dark),
      text: dark ? "text-white" : "text-secondary-800"
    };
  }

  switch (color) {
    case COLOR_1:
      return {
        border: "border-products-50",
        bg: dark ? "bg-products-50" : "bg-products-50/10",
        text: dark ? "text-white" : "text-products-50"
      };
    case COLOR_2:
      return {
        border: "border-products-100",
        bg: dark ? "bg-products-100" : "bg-products-100/10",
        text: dark ? "text-white" : "text-products-100"
      };
    case COLOR_3:
      return {
        border: "border-products-200",
        bg: dark ? "bg-products-200" : "bg-products-200/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_4:
      return {
        border: "border-products-300",
        bg: dark ? "bg-products-300" : "bg-products-300/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_5:
      return {
        border: "border-products-400",
        bg: dark ? "bg-products-400" : "bg-products-400/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_6:
      return {
        border: "border-products-500",
        bg: dark ? "bg-products-500" : "bg-products-500/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_7:
      return {
        border: "border-products-600",
        bg: dark ? "bg-products-600" : "bg-products-600/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_8:
      return {
        border: "border-products-700",
        bg: dark ? "bg-products-700" : "bg-products-700/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_9:
      return {
        border: "border-products-800",
        bg: dark ? "bg-products-800" : "bg-products-800/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    case COLOR_10:
      return {
        border: "border-products-900",
        bg: dark ? "bg-products-900" : "bg-products-900/10",
        text: dark ? "text-white" : "text-primary-900"
      };
    default:
      return {
        border: "border-primary-300",
        bg: dark ? "bg-primary-700" : "bg-primary-100",
        text: dark ? "text-white" : "text-primary-900"
      };
  }
}

export { getColorName, getColorBgClass, getColorList, getGiftCardBgClass, getKioskProductGroupCardClasses };
