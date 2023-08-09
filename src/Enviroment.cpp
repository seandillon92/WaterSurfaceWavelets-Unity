#include "Enviroment.h"
#include "math/ArrayAlgebra.h"
#include "math/interpolation/Interpolation.h"

#include <cmath>
#include <iostream>

namespace WaterWavelets {

auto data_grid = [](int i, int j, size_t N, float* data) -> float {
  // outside of the data grid just return some high arbitrary number
  if (i < 0 || i >= N || j < 0 || j >= N)
    return 100;

  return data[j + i * N];
};

auto dx_data_grid = [](int i, int j, size_t N, float* data) -> float {
  if (i < 0 || i >= (N - 1) || j < 0 || j >= N)
    return 0;

  return data[j + (i + 1) * N] - data[j + i * N];
};

auto dy_data_grid = [](int i, int j, size_t N, float* data) -> float {
  if (i < 0 || i >= N || j < 0 || j >= (N - 1))
    return 0;

  return data[(j + 1) + i * N] - data[j + i * N];
};

auto igrid =
    InterpolationDimWise(LinearInterpolation, LinearInterpolation)(data_grid);

auto igrid_dx = InterpolationDimWise(LinearInterpolation,
                                     LinearInterpolation)(dx_data_grid);

auto igrid_dy = InterpolationDimWise(LinearInterpolation,
                                     LinearInterpolation)(dy_data_grid);

auto grid = [](Vec2 pos, float dx, size_t N, float* data) -> float {
  pos *= 1 / dx;
  pos += Vec2{N / 2 - 0.5f, N / 2 - 0.5f};
  return igrid(pos[1], pos[0], N, data) * dx;
};

auto grid_dx = [](Vec2 pos, float dx, size_t N, float* data) -> float {
  pos *= 1 / dx;
  pos += Vec2{N / 2 - 1.0f, N / 2 - 0.5f};
  return igrid_dx(pos[1], pos[0], N, data);
};

auto grid_dy = [](Vec2 pos, float dx, size_t N, float* data) -> float {
  pos *= 1 / dx;
  pos += Vec2{N / 2 - 0.5f, N / 2 - 1.0f};
  return igrid_dy(pos[1], pos[0], N, data);
};

Environment::Environment(float size, float* data, size_t data_size) {
    _data = new float[data_size];
    memcpy(_data, data, sizeof(float) * data_size);
    N = sqrt(data_size);
    _dx = (2 * size) / N;
}

bool Environment::inDomain(Vec2 pos) const { return levelset(pos) >= 0; }

Real Environment::levelset(Vec2 pos) const { return grid(pos, _dx, N, _data); }

Vec2 Environment::levelsetGrad(Vec2 pos) const {
  Vec2 grad = Vec2{grid_dy(pos, _dx, N, _data), grid_dx(pos, _dx, N, _data)};
  return normalized(grad);
}

} // namespace WaterWavelets
