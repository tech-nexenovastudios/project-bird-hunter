#import <UIKit/UIKit.h>

extern "C" {
    // style: 0 = Light, 1 = Medium, 2 = Heavy
    void _HapticImpact(int style)
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackStyle s;
            switch (style)
            {
                case 0:  s = UIImpactFeedbackStyleLight;  break;
                case 2:  s = UIImpactFeedbackStyleHeavy;  break;
                default: s = UIImpactFeedbackStyleMedium; break;
            }
            UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
            [gen prepare];
            [gen impactOccurred];
        }
    }
}
