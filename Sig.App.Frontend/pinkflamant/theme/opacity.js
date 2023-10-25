const opacity = {};

for (let i = 0; i <= 100; i += 5) {
  opacity[i] = i / 100;
}

module.exports = opacity;
