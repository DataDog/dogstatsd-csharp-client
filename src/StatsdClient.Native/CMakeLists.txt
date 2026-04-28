cmake_minimum_required(VERSION 3.15)
project(fs C)

add_library(fs SHARED fs.c fs.h)

install(TARGETS fs DESTINATION lib)
