#pragma once

#include <array>
#include <vector>

#include "math/Math.h"
#include "Spectrum.h"

namespace WaterWavelets {

/*
 * The class ProfileBuffer is representation of the integral (21) from the
 * paper.
 *
 * It provides two functionalities:
 * 1. Function `precompute` precomputes values of the integral for main input
 * valus of `p` (see paper for the meaning of p)
 * 2. Call operator evaluates returns the profile value at a given point.
 * This is done by interpolating from precomputed values.
 *
 * TODO: Add note about making (21) pariodic over precomputed interval.
 */
class ProfileBuffer {
public:
  /**
   * Precomputes profile buffer for the give spectrum.
   *
   * This function numerically precomputes integral (21) from the paper.
   *
   * @param spectrum A function which accepts zeta(=log2(wavelength)) and retuns
   * wave density.
   * @param time Precompute the profile for a given time.
   * @param zeta_min Lower bound of the integral. zeta_min == log2('minimal
   * wavelength')
   * @param zeta_min Upper bound of the integral. zeta_max == log2('maximal
   * wavelength')
   * @param resolution number of nodes for which the .
   * @param periodicity The period of the final function is determined as
   * `periodicity*pow(2,zeta_max)`
   * @param integration_nodes Number of integraion nodes
   */
  template <typename Spectrum>
  void precompute(
      Spectrum &spectrum, 
      float time, 
      int resolution = 4096, 
      int periodicity = 2) {

    m_data.resize(resolution);
    m_period = periodicity * pow(2, m_zeta_max);

#pragma omp parallel for
    for (int i = 0; i < resolution; i++) {

      constexpr float tau = 6.28318530718;
      float           p   = (i * m_period) / resolution;

      m_data[i] =
          integrate_with_step(m_integration_nodes, m_zeta_min, m_zeta_max, [&](float zeta, int step) {

            float waveLength = pow(2, zeta);
            float waveNumber = tau / waveLength;
            float phase1 =
                waveNumber * p - dispersionRelation(waveNumber) * time;
            float phase2 = waveNumber * (p - m_period) -
                           dispersionRelation(waveNumber) * time;

            float weight1 = p / m_period;
            float weight2 = 1 - weight1;
            return waveLength * m_spectrum_data[step] *
                   (cubic_bump(weight1) * gerstner_wave(phase1, waveNumber) +
                    cubic_bump(weight2) * gerstner_wave(phase2, waveNumber));
          });
    }
  }

  /**
   * Evaluate profile at point p by doing linear interpolation over precomputed
   * data
   * @param p evaluation position, it is usually p=dot(position, wavedirection)
   */
  std::array<float, 4> operator()(float p) const;

private:
  /**
   * Dispersion relation in infinite depth -
   * https://en.wikipedia.org/wiki/Dispersion_(water_waves)
   */
  float dispersionRelation(float k) const;

  /**
   * Gerstner wave - https://en.wikipedia.org/wiki/Trochoidal_wave
   *
   * @return Array of the following values:
      1. horizontal position offset
      2. vertical position offset
      3. position derivative of horizontal offset
      4. position derivative of vertical offset
   */
  std::array<float, 4> gerstner_wave(float phase /*=knum*x*/, float knum) const;

  /** bubic_bump is based on $p_0$ function from
   * https://en.wikipedia.org/wiki/Cubic_Hermite_spline
   */
  float cubic_bump(float x) const;

public:
  float m_period;

  std::vector<std::array<float, 4>> m_data;
private:
    float m_zeta_min;
    float m_zeta_max;
    int m_integration_nodes;
    std::vector<double> m_spectrum_data;
    Spectrum& m_spectrum;
public:
    ProfileBuffer(float z_min, float z_max, int integration_nodes, Spectrum& spectrum);
};

}; // namespace WaterWavelets
