const screens = require("./screens");

// Add '1/7' width for uiGridTiles
const widths = { "1/7": "14.2857143%" };

// Add custom widths for uiBlock container
Object.keys(screens).forEach((key) => {
  widths[key] = `calc(${screens[key]} + var(--pf-section-padding) * 2)`;
});

module.exports = widths;
