#!/usr/bin/env python3
"""Offline source/geometry checks; does not replace Unity or ML2 testing."""
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def main():
    scene = read("Assets/Scenes/M1_SmokeTest.unity")
    registration = read("Assets/Scripts/Board/BoardRegistration.cs")
    condition = read("Assets/Scripts/Experiment/ConditionController.cs")
    calibration = read("Assets/Scripts/Board/BoardDistanceCalibration.cs")
    board = read("Assets/Scripts/Tetris/TetrisBoard.cs")
    config = read("Assets/Scripts/Experiment/ExperimentConfig.cs")
    asset = read("Assets/Config/ExperimentConfig.asset")
    expected = dict(focalDistanceMeters=.74, c1DistanceMeters=.8,
                    c2DistanceMeters=1, c3DistanceMeters=1.5,
                    boardDistanceToleranceMeters=.03, tetrisDurationSeconds=900,
                    depthTaskReferenceDistanceMeters=1, formalDepthTrials=8,
                    practiceDepthTrials=3, depthDifferenceMeters=.06,
                    targetHorizontalSeparationMeters=.12,
                    depthTargetDiameterMeters=.04,
                    targetReferenceScaleDistanceMeters=1,
                    visualAngleReferenceDistanceMeters=1)
    for key, value in expected.items():
        default = re.search(r"\b" + key + r"\s*=\s*([\d.]+)f?;", config)
        serialized = re.search(r"^  " + key + r": ([\d.]+)$", asset, re.M)
        assert default and serialized, key
        assert float(default[1]) == float(serialized[1]) == value, key
    assert "markerSizeMeters = 0.12f" in registration
    assert "estimateQrLength = false" in registration
    assert "markerSizeMeters: 0.12" in scene and "estimateQrLength: 0" in scene
    assert "stimulusOffsetMeters: {x: 0, y: -0.4}" in scene
    assert "new(0f, -0.40f)" in condition
    assert "new Vector3(stimulusOffsetMeters.x, stimulusOffsetMeters.y, 0f)" in condition
    assert "ScaleFactor = targetDistance / referenceDistance" in condition
    assert "blockPrefab: {fileID: 0}" in scene
    lock = calibration.split("public void LockBoardPose()", 1)[1].split("public void RescanBoard", 1)[0]
    assert lock.index("RefreshMeasurement();") < lock.index("CurrentState != BoardDistanceState.Ready")
    assert "ActualDistanceMeters = Vector3.Distance(viewer.position, boardAnchor.position)" in calibration
    assert "Mathf.Abs(Vector3.Dot(eyeToAnchor, boardAnchor.forward))" in calibration
    # Derive the numerical mesh check from the implementation's actual arrays.
    points = dict((name, tuple(float(x.rstrip('f')) for x in values.split(',')))
                  for name, values in re.findall(r"Vector3 ([abcd]) = new\(([^)]+)\)", board))
    names = re.search(r"planarMesh.vertices = new\[\] \{ ([^}]+) \}", board)[1]
    vertices = [points[name.strip()] for name in names.split(',')]
    indices = [int(x) for x in re.search(r"planarMesh.triangles = new\[\] \{ ([^}]+) \}", board)[1].split(',')]
    assert all(p[2] == 0 for p in vertices)
    signed_areas = []
    for i in range(0, len(indices), 3):
        a, b, c = [vertices[j] for j in indices[i:i+3]]
        signed_areas.append((b[0]-a[0])*(c[1]-a[1]) - (b[1]-a[1])*(c[0]-a[0]))
    assert len(signed_areas) == 4 and all(abs(a) == 1 for a in signed_areas)
    assert sum(a > 0 for a in signed_areas) == sum(a < 0 for a in signed_areas) == 2
    # Defaults: 20 rows, 2 cm cells, 2 mm border, max scale 1.5, 12 cm QR.
    gap = .4 - (20 * .02 / 2 + .002) * 1.5 - .12 / 2
    assert math.isclose(gap, .037)
    print("PASS: 14 code/asset candidate values; fixed 12 cm QR; scene placement;")
    print("      LOCK remeasurement; preserved Actual definition; double-sided Z=0 mesh.")
    print(f"Default maximum-scale QR-pattern clearance: {gap*1000:.1f} mm (excludes white border).")
    for d in (.8, 1, 1.5):
        print(f"QR-normal example: plane/anchor={d:.2f} m, stimulus={math.hypot(d,.4):.4f} m")
    print("NOT TESTED: Unity API compilation, rendering, gameplay, tracking, physical accuracy.")


if __name__ == "__main__":
    main()
