# StrangeLand Vehicle (UPM)

A Unity package providing a **ready-to-use reference vehicle (Jaguar XE)** for StrangeLand, including a drivable prefab with mirrors, audio, speedometer, and integration hooks for StrangeLand input and networking.

- Works out of the box with keyboard input
- Optional integration with **StrangeLand Steering** for steering wheels

---

## Install (Unity Package Manager)

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.strangeland.vehicle": "https://github.com/Strange-Land/strangeland.vehicle.git"
  }
}
````

Pin a version if needed:

```json
{
  "dependencies": {
    "com.strangeland.vehicle": "https://github.com/Strange-Land/strangeland.vehicle.git#<tag-or-commit>"
  }
}
```

**Recommended packages**

Install together for a full StrangeLand setup:

* `com.strangeland.core`
* `com.strangeland.steering` *(optional for wheel hardware)*
* `com.strangeland.vehicle`

---

## Import the Vehicle

Open:

**Window → Package Manager → StrangeLand Vehicle → Samples**

Import **XE Prefab**.

Unity copies the assets into:

```
Assets/Samples/com.strangeland.vehicle/<version>/XE_Prefab
```

Then:

1. Drag the vehicle prefab into your scene **or**
2. Register `Car_InteractableObject.asset` in your **ConnectionAndSpawning** configuration.

---

## Steering Wheel Support (Optional)

For real steering wheels:

1. Install **StrangeLand Steering**
   [https://github.com/Strange-Land/strangeland.steering](https://github.com/Strange-Land/strangeland.steering)

2. Import a steering provider (e.g., Logitech) via its **Samples**

The vehicle will automatically read input from `SteeringWheelManager`.

---

## Notes

* The XE vehicle is a **reference implementation** — duplicate and modify it as needed.
* For best visuals, use **baked lighting** and optional **reflection probes**.
* If audio errors appear, ensure mixer groups are assigned in the prefab hierarchy.
