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

export { getColorName, getColorBgClass, getColorList };
