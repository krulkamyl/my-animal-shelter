<?php

namespace App\Repositories\Shelter;

use Illuminate\Database\Eloquent\Collection;
use Illuminate\Database\Eloquent\Model;

interface ShelterRepositoryInterface
{
    public function getById($id): Shelter|Collection|Model|null;

    public function getAll(): Collection;

    public function create(array $data): Shelter;

    public function update($id, array $data): Shelter|Model|null;
}
