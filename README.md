# Fable.Yjs

## Repro

1. ```
   cd ./packages/Fable.Yjs.Adaptive
   ```
1. ```
   npm install && dotnet tool restore && dotnet restore
   ```  
1. ```
   npm run dev
   ```

## Expected

```
.> cmd /C node x/Test.js
Watching ..
setting via ytext
ytext callback
cval is set via ytext
ytext is set via ytext
setting via cval
cval callback
cval is set via cval
ytext is set via cval
```

## Actual

```
.> cmd /C node x/Test.js
Watching ..
setting via ytext
ytext callback
cval is set via ytext
ytext is set via ytext
setting via cval
cval callback
cval is set via cval
ytext is set via ytext
```
