# AgLibrary Controls

Custom Windows Forms controls for AgOpenGPS. These controls provide enhanced functionality for touch-friendly interfaces and improved user experience.

## Controls

### NudlessNumericUpDown

A touch-friendly numeric input control designed for tablet use in tractors.

**File**: `NudlessNumericUpDown.cs`

**Purpose**: Replace spinner-based numeric input with a full-screen keypad that works better with gloves and bouncing tractor movement.

**Key Features**:
- Button-based control (not a spinner)
- Opens full-screen numeric keypad on click
- Automatic unit conversion (imperial/metric)
- Designer support with property preview
- Auto-sizing font to fit control bounds

**Usage**:
```csharp
using AgLibrary.Controls;

var control = new NudlessNumericUpDown();
control.Mode = UnitMode.Small;  // inches/cm conversion
control.Minimum = 0;
control.Maximum = 100;
control.DecimalPlaces = 2;
control.Value = 25.5;

// Required: Wire up numeric form creator
control.CreateNumericForm = (min, max, val) => new FormNumeric(min, max, val);

// Required: Set up unit conversion delegates
control.GetDisplayConversionFactor = (mode) => {
    return mode switch {
        UnitMode.Small => m2InchOrCm,    // meters to inches/cm
        UnitMode.Large => m2FtOrM,        // meters to feet/meters
        UnitMode.Speed => kmhToMphOrKmh,  // km/h to mph/km/h
        _ => 1.0
    };
};

control.GetStorageConversionFactor = (mode) => {
    return mode switch {
        UnitMode.Small => inchOrCm2m,     // inches/cm to meters
        UnitMode.Large => ftOrMtoM,       // feet/meters to meters
        UnitMode.Speed => mphOrKmhToKmh,  // mph/km/h to km/h
        _ => 1.0
    };
};

control.ValueChanged += (s, e) => {
    Console.WriteLine($"Value changed to: {control.Value}");
};
```

**Unit Modes**:
- `None`: No conversion (1:1)
- `Large`: Meters ↔ Feet/Meters (for large distances)
- `Small`: Meters ↔ Inches/Centimeters (for small measurements)
- `Speed`: Km/h ↔ Mph/Km/h (for speed)
- `Area`: Reserved for future use
- `Distance`: Reserved for future use
- `Temperature`: Reserved for future use

**Properties**:
- `Value` (double): Current value in storage units
- `Minimum` (double): Minimum allowed value
- `Maximum` (double): Maximum allowed value
- `DecimalPlaces` (int): Number of decimal places to display
- `Mode` (UnitMode): Unit conversion mode

**Designer Support**:
When setting properties in the Visual Studio designer, the control displays: `min|max|mode`

Example: `0|100|Small` means min=0, max=100, mode=Small (inches/cm)

---

### ListViewItemSorter

Smart ListView column sorter that automatically detects data types.

**File**: `ListViewItemSorter.cs`

**Purpose**: Provide intelligent sorting for ListView controls without manual type specification.

**Key Features**:
- Automatic type detection (numeric, date, string)
- Correct sorting for each type
- Click column header to toggle sort order
- Numbers always sort before non-numbers

**Usage**:
```csharp
using AgLibrary.Controls;

var sorter = new ListViewItemSorter(myListView);

// That's it! Sorter automatically:
// - Attaches to ListView
// - Handles ColumnClick events
// - Detects column types
// - Sorts appropriately
```

**Sorting Behavior**:
- **Numeric columns**: Sorted numerically (1, 2, 10, not 1, 10, 2)
- **Date columns**: Sorted chronologically (descending by default)
- **String columns**: Sorted alphabetically (case-insensitive)
- **Mixed columns**: Numbers on top, then strings
- **Toggle**: Click same column header to reverse order
- **New column**: Click different column to sort ascending

**Example ListView**:
```
Name        | Size (MB) | Date Modified
------------|-----------|------------------
File1.txt   | 1.5       | 2025-10-10 10:00
File10.txt  | 15        | 2025-10-09 14:30
File2.txt   | 2.3       | 2025-10-08 09:15
```

After clicking "Size (MB)" header: Sorts numerically (1.5, 2.3, 15)
After clicking "Date Modified" header: Sorts by date (newest first)
After clicking "Name" header: Sorts alphabetically (File1, File2, File10)

---

### RepeatButton

Button that repeats its action while held down.

**File**: `RepeatButton.cs`

**Purpose**: Provide increment/decrement functionality for numeric values.

**Key Features**:
- Fires initial action on mouse down
- Repeats action while button is held
- Configurable initial delay and repeat interval

**Usage**:
```csharp
using AgLibrary.Controls;

var button = new RepeatButton();
button.InitialDelay = 400;      // ms before first repeat
button.RepeatInterval = 62;     // ms between repeats
button.Text = "+";

button.Click += (s, e) => {
    currentValue++;
    UpdateDisplay();
};
```

**Properties**:
- `InitialDelay` (int): Time in ms before first repeat (default: 400)
- `RepeatInterval` (int): Time in ms between repeats (default: 62)

---

## Integration Notes

### For AgOpenGPS Developers

#### Current Status (2025-10-10)

✅ **IMPLEMENTED**: The enhanced `NudlessNumericUpDownEx` wrapper class is ready to use in new forms.

⚠️ **NOT YET MIGRATED**: Existing forms still use the legacy `NudlessNumericUpDown` control for backward compatibility.

#### How to Use NudlessNumericUpDownEx

The wrapper class is already implemented in `SourceCode/GPS/Classes/CExtensionMethods.cs`:

```csharp
using AgOpenGPS;

// In your form designer or code:
var control = new NudlessNumericUpDownEx();
control.Mode = AgLibrary.Controls.UnitMode.Small;  // Auto unit conversion
control.Minimum = 0;
control.Maximum = 100;
control.DecimalPlaces = 2;
control.Value = 25.5;

control.ValueChanged += (s, e) => {
    // Handle value changes
    double newValue = control.Value;  // Note: double, not decimal
};
```

The wrapper automatically:
- Wires up `FormNumeric` for the keypad
- Gets unit conversion factors from `FormGPS`
- Supports `UnitMode.Small` (inches/cm) and `UnitMode.Large` (feet/meters)

#### Migration Path (For Future Work)

To migrate an existing form from legacy `NudlessNumericUpDown` to `NudlessNumericUpDownEx`:

1. **Replace the control type** in the designer file:
   ```csharp
   // Old
   private NudlessNumericUpDown nudDistance;

   // New
   private NudlessNumericUpDownEx nudDistance;
   ```

2. **Update value handling** (decimal → double):
   ```csharp
   // Old
   decimal value = nudDistance.Value;

   // New
   double value = nudDistance.Value;
   ```

3. **Remove ShowKeypad() calls** - the enhanced control handles clicks automatically:
   ```csharp
   // Old
   private void nudDistance_Click(object sender, EventArgs e)
   {
       ((NudlessNumericUpDown)sender).ShowKeypad(this);
       // ... save value ...
   }

   // New
   private void nudDistance_Click(object sender, EventArgs e)
   {
       // No need to call ShowKeypad - handled automatically
       // Just save the value
   }
   ```

4. **Set UnitMode** if appropriate:
   ```csharp
   nudDistance.Mode = AgLibrary.Controls.UnitMode.Small;  // For cm/inch values
   ```

#### Forms Ready for Migration

The following forms have simple NudlessNumericUpDown controls that are good candidates:

- `FormNudge` - 1 control (nudSnapDistance) - snap distance setting
- `FormRefNudge` - reference line nudge
- `FormQuickAB` - quick AB line setup

#### Type Compatibility

**Important**: The enhanced control uses `double` values, while the legacy control uses `decimal`.

```csharp
// Legacy control
NudlessNumericUpDown (inherits NumericUpDown)
- Value type: decimal
- Range: -79,228,162,514,264,337,593,543,950,335 to 79,228...
- Precision: 28-29 significant digits

// Enhanced control
NudlessNumericUpDownEx (inherits Button)
- Value type: double
- Range: ±5.0 × 10^−324 to ±1.7 × 10^308
- Precision: 15-17 significant digits

For agricultural measurements (meters, centimeters), double provides more than enough precision.
```

### For Other Applications

If using these controls outside AgOpenGPS:

1. Implement the required delegates for `NudlessNumericUpDown`
2. Create a form with `ReturnValue` property for numeric input
3. Wire up conversion factors based on your application's settings

Example for custom application:
```csharp
var control = new AgLibrary.Controls.NudlessNumericUpDown();

control.CreateNumericForm = (min, max, val) => {
    // Return your custom numeric input form
    return new MyNumericForm(min, max, val);
};

control.GetDisplayConversionFactor = (mode) => {
    // Return conversion factor for display
    if (mode == UnitMode.Small && useImperial)
        return 39.3701; // meters to inches
    return 1.0;
};

control.GetStorageConversionFactor = (mode) => {
    // Return inverse conversion for storage
    if (mode == UnitMode.Small && useImperial)
        return 0.0254; // inches to meters
    return 1.0;
};
```

## Source

These controls were ported from AOG_Dev with improvements:
- Decoupled from specific form dependencies
- Added delegate-based architecture for flexibility
- Enhanced documentation
- Maintained all original functionality

## Testing

### Manual Testing Checklist

**NudlessNumericUpDown**:
- [ ] Click control opens numeric keypad
- [ ] Enter value within range accepts value
- [ ] Enter value below minimum shows error
- [ ] Enter value above maximum shows error
- [ ] Unit conversion displays correctly
- [ ] Value changes fire ValueChanged event
- [ ] Designer properties update control display

**ListViewItemSorter**:
- [ ] Click numeric column sorts numerically
- [ ] Click date column sorts chronologically
- [ ] Click string column sorts alphabetically
- [ ] Click same column reverses sort order
- [ ] Mixed content columns sort correctly

**RepeatButton**:
- [ ] Click fires action once
- [ ] Hold fires initial action after InitialDelay
- [ ] Hold continues firing every RepeatInterval
- [ ] Release stops firing actions

## License

Same as AgOpenGPS project.
