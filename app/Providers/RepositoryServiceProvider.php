<?php

namespace App\Providers;

use App\Repositories\Shelter\ShelterRepository;
use App\Repositories\Shelter\ShelterRepositoryInterface;
use Illuminate\Support\ServiceProvider;

class RepositoryServiceProvider extends ServiceProvider
{
    public function register(): void {}

    public function boot(): void
    {
        $this->app->bind(
            ShelterRepositoryInterface::class, ShelterRepository::class
        );
    }
}
