#pragma once

#include "Global.h"

namespace WaterWavelets {

class Environment {
public:
  Environment(float size, float* data, size_t data_size);

  bool inDomain(Vec2 pos) const;
  Real levelset(Vec2 pos) const;
  Vec2 levelsetGrad(Vec2 pos) const;

public:
  float _dx;
private:
  float* _data;
  int N;
};

} // namespace WaterWavelets
