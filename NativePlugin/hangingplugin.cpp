#include <iostream>

extern "C"
{
    void HelloFromCpp()
    {
        std::cout << "Hello from C++!" << std::endl;
    }

    // Spins forever on purpose. The flag is volatile so the compiler can't
    // treat the loop as side-effect free and optimize it away.
    void Hang()
    {
        volatile bool keepSpinning = true;
        while (keepSpinning)
        {
        }
    }
}
