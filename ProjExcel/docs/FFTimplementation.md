Here’s the straight, practical answer you need as someone building a **C# Excel‑DNA add‑in** with scientific workloads (FFT, seismic transforms, beamforming, CFP, Radon, etc.).  

You have **three viable ways** to include FFT routines, but only **one** is truly optimal for Excel‑DNA.

---

## ⭐ The best FFT option for a C# Excel‑DNA add‑in  
### **Math.NET Numerics (with MKL or OpenBLAS native provider)**  
This is the most stable, most widely used, and most Excel‑DNA‑friendly FFT implementation available in .NET.

It gives you:

- **Complex FFT** (1D, 2D, arbitrary length)  
- **Real FFT**  
- **Windowing**  
- **Convolution / correlation**  
- **Native acceleration** (MKL, OpenBLAS)  
- **No unsafe code**  
- **No P/Invoke headaches**  
- **Works perfectly inside Excel‑DNA**  

And it’s actively maintained.

### Install:
```
Install-Package MathNet.Numerics
Install-Package MathNet.Numerics.MKL.Win   // optional, fastest
```

### Example FFT call:
```csharp
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using System.Numerics;

Complex[] data = new Complex[1024];
// fill data...

Fourier.Forward(data, FourierOptions.Matlab);
```

This works flawlessly inside Excel‑DNA.

---

## ⭐ Why Math.NET is the best choice for Excel‑DNA  
Because Excel‑DNA runs inside **Excel.exe**, you want:

- **pure managed code**  
- **no unmanaged DLL loading issues**  
- **no LoaderLock problems**  
- **no AVX/FFT native DLL conflicts**  
- **no dependency hell**  

Math.NET avoids all of these.

And if you add MKL:

- FFTs become **extremely fast**  
- perfect for seismic workloads  
- no need to write your own FFT kernels  

---

## ⭐ Alternative options (and why they’re worse)

### 1. **System.Numerics.FFT**  
Does not exist.  
.NET has no built‑in FFT.

### 2. **Accord.NET**  
Has FFT, but:

- project is abandoned  
- no MKL acceleration  
- slower  
- not recommended for new scientific code  

### 3. **FFTW via P/Invoke**  
Fast, but:

- unmanaged DLL  
- requires redistribution  
- Excel‑DNA loader issues  
- LoaderLock warnings  
- AVX CPU mismatch crashes  
- not worth the pain  

### 4. **Custom FFT implementation**  
You *can* write your own Cooley‑Tukey FFT, but:

- slower  
- more code  
- no SIMD  
- no MKL  
- no benefit over Math.NET  

---

## ⭐ Summary: Best choice for your Excel‑DNA add‑in

| Option | Speed | Excel‑DNA Safety | Maintenance | Recommendation |
|--------|-------|------------------|-------------|----------------|
| **Math.NET + MKL** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | **Best** |
| Math.NET (managed only) | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Very good |
| FFTW (native) | ⭐⭐⭐⭐⭐ | ⭐ | ⭐ | Avoid |
| Accord.NET | ⭐⭐ | ⭐⭐⭐ | ⭐ | Avoid |
| Custom FFT | ⭐ | ⭐⭐⭐⭐⭐ | ⭐ | Only for teaching |

**Use Math.NET Numerics.**  
It’s the right tool for Excel‑DNA scientific add‑ins.

---

## If you want, I can generate:
- a **ready‑to‑use Excel‑DNA UDF** for FFT  
- a **complex FFT wrapper** that returns magnitude/phase  
- a **2D FFT** for seismic shot gathers  
- a **Radon transform** using FFT acceleration  
- a **beamforming kernel** using FFT steering vectors  

Just tell me what FFT operation you want to expose to Excel.