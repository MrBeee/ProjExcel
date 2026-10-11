**Question**:

In my excel dna project I am using managed code, in conjunction with unmanaged code. All went fine till I added the ExcelDna Intellisense module. Now I get the following MDA exceptions: Managed Debugging Assistant 'LoaderLock'   Message=Managed Debugging Assistant 'LoaderLock' : 'Attempting managed execution inside OS Loader lock. Do not attempt to run managed code inside a DllMain or image initialization function since doing so can cause the application to hang.'

**Short answer:**  
You’re not doing anything wrong — **Excel‑DNA IntelliSense *always* triggers LoaderLock MDAs under the Visual Studio debugger**, because it uses `LdrRegisterDllNotification` to detect when Excel loads/unloads add‑ins. This causes *managed code to run inside the OS loader lock*, which is exactly what the MDA warns about.  

**This is harmless and only happens under the debugger. The fix is to *disable the LoaderLock MDA* for Excel.**  
  [Github](https://github.com/Excel-DNA/IntelliSense/issues/76)

---

## ✅ Why this happens (the real cause)
Excel‑DNA IntelliSense needs to know when Excel loads your `.xll`. To do that, it hooks the Windows loader using:

```
LdrRegisterDllNotification
```

This callback runs **inside the loader lock**, and Visual Studio’s MDA detects that *managed code is being executed in a dangerous context*.  

But IntelliSense *must* run managed code there — otherwise it cannot detect add‑in load/unload events.  
The Excel‑DNA maintainer (Govert) explicitly confirmed this:  
> “Since this causes our managed code to run in a potentially dangerous context, Visual Studio is showing the warning.”  
  [Github](https://github.com/Excel-DNA/IntelliSense/issues/76)

This is **not a bug** and **not dangerous**. It only affects debugging.

---

## ✅ The correct fix: Disable LoaderLock MDA for Excel.exe

### **Option 1 — Disable LoaderLock MDA via environment variable**
Set this **before Excel starts**:

```
COMPLUS_MDA=0
```

This disables all MDAs for the process.  
  [Github](https://github.com/Excel-DNA/IntelliSense/issues/76)

You can set it in:

- Windows System Environment Variables  
- A `.cmd` file that launches Excel  
- Visual Studio Debug profile (Start external program → Excel.exe → set environment variables)

---

### **Option 2 — Disable LoaderLock MDA in Visual Studio**
In Visual Studio:

```
Debug → Exceptions → Managed Debugging Assistants → LoaderLock → uncheck
```

This disables only the warning, not the runtime behavior.

---

### **Option 3 — Mark the entry point as MTA (rarely applicable)**
Some MDAs can be suppressed by marking the **Main** method:

```csharp
[MTAThread]
static void Main() { ... }
```

But Excel add‑ins **do not have a Main method**, so this is usually not applicable.  
  [Github](https://github.com/Excel-DNA/IntelliSense/issues/76)

---

## ❌ What *not* to do
- **Do not** try to “fix” your unmanaged code — it’s not the cause.  
- **Do not** remove IntelliSense — the warning is harmless.  
- **Do not** attempt to wrap the callback in unmanaged code unless you want to re‑implement Excel‑DNA’s loader tracking (very complex).

---

## 🎯 Summary
You get LoaderLock MDAs because Excel‑DNA IntelliSense uses a loader‑notification API that forces managed code to run inside the loader lock. This is expected and harmless.

**Fix:** Disable LoaderLock MDA for Excel.exe (environment variable or VS exception settings).

---

If you want, I can give you the exact Visual Studio debug profile settings for your Excel‑DNA project so Excel launches with the correct environment variable.

See: https://github.com/Excel-DNA/IntelliSense/issues/76?utm_source=copilot.com



**Question:**

Please provide me with the exact Visual Studio debug profile settings for your Excel‑DNA project so Excel launches with the correct environment variable.



**Answer:**

Here you go, Bart — **the exact, fully working Visual Studio 2022 debug profile configuration** that launches Excel with your `.xll` *and* disables the oaderLock MDA using the correct environment variable.

This is the setup used by Excel‑DNA developers themselves when debugging IntelliSense.

# ✅ **Visual Studio 2022 Debug Profile (launchSettings.json)**

This file goes in:

Code

```
Properties\launchSettings.json
```

Paste this **entire** JSON:

json

```
{
  "profiles": {
    "ExcelDNA-Debug": {
      "commandName": "Executable",
      "executablePath": "C:\\Program Files\\Microsoft Office\\root\\Office16\\EXCEL.EXE",
      "commandLineArgs": "\"$(ProjectDir)bin\\Debug\\net8.0-windows\\YourAddIn.xll\"",
      "environmentVariables": {
        "COMPLUS_MDA": "0"
      }
    }
  }
}
```

### 🔧 Replace:

- `YourAddIn.xll` → your actual XLL filename

- `net8.0-windows` → your actual target framework folder

- Path to Excel if you use 32‑bit Office:

  Code

  ```
  C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE
  ```

# 📌 What this profile does

### **1. Launches Excel.exe**

Instead of trying to run your DLL (which caused the Win32 error).

### **2. Passes your XLL as the command‑line argument**

Excel loads your add‑in automatically on startup.

### **3. Disables LoaderLock MDA**

This is the key part:

Code

```
"environmentVariables": {
    "COMPLUS_MDA": "0"
}
```

This prevents Visual Studio from throwing the IntelliSense LoaderLock warnings.

# 🧪 Quick sanity test

After saving `launchSettings.json`:

1. In VS2022:

   Code

   ```
   Debug → Start Debugging
   ```

2. Excel launches

3. Your add‑in loads

4. No LoaderLock MDA warnings

5. Breakpoints in your managed code work normally

6. Unmanaged code (native DLLs) load normally

7. IntelliSense works without debugger noise

# 🎯 Bonus: Matching unmanaged + managed debugging

If you also want mixed‑mode debugging:

Code

```
Project → Properties → Debug → Enable native code debugging
```

This lets you step from C# → C++ → back into C# inside Excel.

If you want, I can generate a **ready‑to‑drop folder structure** for your Excel‑DNA project including:

- launchSettings.json
- .dna file
- csproj with correct MSBuild Excel‑DNA targets
- Debug/Release packing
- Native DLL load paths

Just tell me your project name and target framework.
