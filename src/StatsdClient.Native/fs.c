#include "fs.h"
#include <sys/stat.h>

int get_inode(const char* path, unsigned long long* ino)
{
    struct stat s;
    if (stat(path, &s) != 0) return -1;
    *ino = (unsigned long long)s.st_ino;
    return 0;
}
